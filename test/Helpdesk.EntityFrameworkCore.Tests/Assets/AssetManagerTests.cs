using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Helpdesk.Assets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Xunit;

namespace Helpdesk.EntityFrameworkCore.Assets;

public class AssetManagerTests : HelpdeskEntityFrameworkCoreTestBase, IDisposable
{
    // Fixed timestamp so the generated asset tag prefix is deterministic (AST-209901-).
    // Seeded assets use AST-20260901- prefix, so no collision.
    private static readonly DateTime _fixedNow = new DateTime(2099, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private const string _prefix = "AST-20990115-";

    // ------------------------------------------------------------------
    // GenerateAssetTagAsync
    // ------------------------------------------------------------------

    [Fact]
    public async Task GenerateAssetTag_ShouldStartAtFirstSequenceForNewMonth()
    {
        var mgr = BuildAssetManager();

        var tag = await WithUnitOfWorkAsync(async () => await mgr.GenerateAssetTagAsync());

        tag.ShouldBe($"{_prefix}0001");
    }

    [Fact]
    public async Task GenerateAssetTag_ShouldIncrementWhenAssetsExist()
    {
        await SeedAssetAsync($"{_prefix}0001", "Test Asset 1");
        await SeedAssetAsync($"{_prefix}0002", "Test Asset 2");
        await SeedAssetAsync($"{_prefix}0003", "Test Asset 3");

        var mgr = BuildAssetManager();
        var tag = await WithUnitOfWorkAsync(async () => await mgr.GenerateAssetTagAsync());

        tag.ShouldBe($"{_prefix}0004");
    }

    [Fact]
    public async Task GenerateAssetTag_ShouldCountSoftDeletedAssets()
    {
        await SeedAssetAsync($"{_prefix}0001", "Test Asset 1");
        var deleted = await SeedAssetAsync($"{_prefix}0002", "Test Asset 2");

        var assetRepo = GetRequiredService<IRepository<Asset, Guid>>();
        await WithUnitOfWorkAsync(async () => await assetRepo.DeleteAsync(deleted, autoSave: true));

        var mgr = BuildAssetManager();
        var tag = await WithUnitOfWorkAsync(async () => await mgr.GenerateAssetTagAsync());

        tag.ShouldBe($"{_prefix}0003");
    }

    [Fact]
    public async Task GenerateAssetTag_ShouldNotCollideWithExistingTag()
    {
        await SeedAssetAsync($"{_prefix}0001", "Test Asset 1");

        var mgr = BuildAssetManager();
        var tag = await WithUnitOfWorkAsync(async () => await mgr.GenerateAssetTagAsync());

        tag.ShouldNotBe($"{_prefix}0001");
        tag.ShouldBe($"{_prefix}0002");
    }

    [Fact]
    public async Task GenerateAssetTag_ShouldSkipGapsFromSoftDeletes()
    {
        // Seed 0001 live, 0002 soft-deleted, leaving a gap.
        await SeedAssetAsync($"{_prefix}0001", "Test Asset 1");
        var deleted = await SeedAssetAsync($"{_prefix}0002", "Test Asset 2");

        var assetRepo = GetRequiredService<IRepository<Asset, Guid>>();
        await WithUnitOfWorkAsync(async () => await assetRepo.DeleteAsync(deleted, autoSave: true));

        var mgr = BuildAssetManager();
        var tag = await WithUnitOfWorkAsync(async () => await mgr.GenerateAssetTagAsync());

        // Count() returns 2 (including soft-deleted), so nextSeq = 3.
        tag.ShouldBe($"{_prefix}0003");
    }

    // ------------------------------------------------------------------
    // CheckSerialNumberUniqueAsync
    // ------------------------------------------------------------------

    [Fact]
    public async Task CheckSerialNumberUnique_ShouldPassWhenSerialIsNull()
    {
        var mgr = BuildAssetManager();

        await WithUnitOfWorkAsync(async () => await mgr.CheckSerialNumberUniqueAsync(null));
    }

    [Fact]
    public async Task CheckSerialNumberUnique_ShouldPassWhenSerialIsEmpty()
    {
        var mgr = BuildAssetManager();

        await WithUnitOfWorkAsync(async () => await mgr.CheckSerialNumberUniqueAsync("   "));
    }

    [Fact]
    public async Task CheckSerialNumberUnique_ShouldThrowWhenSerialExistsForAnotherAsset()
    {
        await SeedAssetAsync("AST-209902-0001", "Serial Test Asset", serial: "TEST-SN-DUP");

        var mgr = BuildAssetManager();

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            async () => await WithUnitOfWorkAsync(async () => await mgr.CheckSerialNumberUniqueAsync("TEST-SN-DUP")));
        ex.Message.ShouldContain("TEST-SN-DUP");
    }

    [Fact]
    public async Task CheckSerialNumberUnique_ShouldNotThrowWhenSerialBelongsToSameAsset()
    {
        var asset = await SeedAssetAsync("AST-209902-0001", "Self Serial Test", serial: "TEST-SN-SELF");

        var mgr = BuildAssetManager();

        await WithUnitOfWorkAsync(async () => await mgr.CheckSerialNumberUniqueAsync("TEST-SN-SELF", asset.Id));
    }

    // ------------------------------------------------------------------
    // CreateAsync
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_AutoGeneratesTag_WhenTagIsNull()
    {
        var mgr = BuildAssetManager();
        var assetRepo = GetRequiredService<IRepository<Asset, Guid>>();

        var asset = await WithUnitOfWorkAsync(async () =>
        {
            var a = await mgr.CreateAsync(null, "New Laptop", AssetType.Laptop, serialNumber: "TEST-SN-AUTO1");
            await assetRepo.InsertAsync(a);
            return a;
        });

        asset.AssetTag.ShouldStartWith(_prefix);
        asset.AssetTag.ShouldBe($"{_prefix}0001");
        asset.Name.ShouldBe("New Laptop");
        asset.AssetType.ShouldBe(AssetType.Laptop);
        asset.Status.ShouldBe(AssetStatus.InStock);
    }

    [Fact]
    public async Task CreateAsync_ShouldUseProvidedTag()
    {
        var mgr = BuildAssetManager();
        var assetRepo = GetRequiredService<IRepository<Asset, Guid>>();

        var asset = await WithUnitOfWorkAsync(async () =>
        {
            var a = await mgr.CreateAsync("AST-CUSTOM-001", "Custom Tag Asset", AssetType.Desktop);
            await assetRepo.InsertAsync(a);
            return a;
        });

        asset.AssetTag.ShouldBe("AST-CUSTOM-001");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowWhenTagAlreadyExists()
    {
        await SeedAssetAsync("AST-DUP-TAG-001", "Existing Tag Asset");

        var mgr = BuildAssetManager();

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            async () => await WithUnitOfWorkAsync(async () =>
                await mgr.CreateAsync("AST-DUP-TAG-001", "Duplicate Tag Asset", AssetType.Monitor)));
        ex.Message.ShouldContain("AST-DUP-TAG-001");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowWhenSerialAlreadyExists()
    {
        await SeedAssetAsync("AST-209903-0001", "Serial Owner", serial: "TEST-SN-CREATE");

        var mgr = BuildAssetManager();

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            async () => await WithUnitOfWorkAsync(async () =>
                await mgr.CreateAsync(null, "New Asset", AssetType.Laptop, serialNumber: "TEST-SN-CREATE")));
        ex.Message.ShouldContain("TEST-SN-CREATE");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCreatedActivity()
    {
        var mgr = BuildAssetManager();
        var assetRepo = GetRequiredService<IRepository<Asset, Guid>>();

        var asset = await WithUnitOfWorkAsync(async () =>
        {
            var a = await mgr.CreateAsync(null, "Activity Test", AssetType.Monitor,
                creatorUserId: Guid.NewGuid(), creatorUserName: "Test User");
            await assetRepo.InsertAsync(a);
            return a;
        });

        var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
        var allActivities = await WithUnitOfWorkAsync(async () => await activityRepo.GetListAsync());
        var activities = allActivities.Where(a => a.AssetId == asset.Id).ToList();

        activities.Count.ShouldBe(1);
        activities[0].ActivityType.ShouldBe(AssetActivityType.Created);
        activities[0].Title.ShouldBe("Khởi tạo thiết bị");
        activities[0].PerformedByUserName.ShouldBe("Test User");
    }

    [Fact]
    public async Task CreateAsync_ShouldSetAllProperties()
    {
        var mgr = BuildAssetManager();
        var assetRepo = GetRequiredService<IRepository<Asset, Guid>>();
        var purchaseDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var warrantyDate = new DateTime(2028, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        var asset = await WithUnitOfWorkAsync(async () =>
        {
            var a = await mgr.CreateAsync(null, "Full Asset", AssetType.ServerStorage,
                serialNumber: "SRV-001", model: "Dell R750", manufacturer: "Dell",
                location: "Server Room", purchaseDate: purchaseDate,
                warrantyExpiryDate: warrantyDate, purchaseCost: 99999m,
                specifications: "4x Xeon, 512GB RAM", notes: "Test notes");
            await assetRepo.InsertAsync(a);
            return a;
        });

        asset.SerialNumber.ShouldBe("SRV-001");
        asset.Model.ShouldBe("Dell R750");
        asset.Manufacturer.ShouldBe("Dell");
        asset.Location.ShouldBe("Server Room");
        asset.PurchaseDate.ShouldBe(purchaseDate);
        asset.WarrantyExpiryDate.ShouldBe(warrantyDate);
        asset.PurchaseCost.ShouldBe(99999m);
        asset.Specifications.ShouldBe("4x Xeon, 512GB RAM");
        asset.Notes.ShouldBe("Test notes");
    }

    // ------------------------------------------------------------------
    // AssignAsync
    // ------------------------------------------------------------------

    [Fact]
    public async Task AssignAsync_ShouldSetAssignmentInfoAndStatus()
    {
        var asset = await SeedAssetAsync("AST-209910-0001", "Assign Test", status: AssetStatus.InStock);
        var userId = Guid.NewGuid();

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.AssignAsync(asset, userId, "John Doe", "john@test.com", "IT Dept", "Laptop for dev", null, null));

        asset.AssignedToUserId.ShouldBe(userId);
        asset.AssignedToUserName.ShouldBe("John Doe");
        asset.AssignedToUserEmail.ShouldBe("john@test.com");
        asset.Department.ShouldBe("IT Dept");
        asset.Status.ShouldBe(AssetStatus.Assigned);
        asset.AssignedDate.ShouldNotBeNull();
    }

    [Fact]
    public async Task AssignAsync_ShouldCreateAssignedActivity()
    {
        var asset = await SeedAssetAsync("AST-209910-0002", "Assign Activity Test", status: AssetStatus.InStock);
        var performerId = Guid.NewGuid();

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.AssignAsync(asset, Guid.NewGuid(), "Jane Smith", "jane@test.com", "HR", null, performerId, "Performer"));

        var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
        var allActivities = await WithUnitOfWorkAsync(async () => await activityRepo.GetListAsync());
        var activities = allActivities.Where(a => a.AssetId == asset.Id).ToList();

        var assignActivity = activities.FirstOrDefault(a => a.ActivityType == AssetActivityType.Assigned);
        assignActivity.ShouldNotBeNull();
        assignActivity!.PerformedByUserId.ShouldBe(performerId);
        assignActivity.PerformedByUserName.ShouldBe("Performer");
        assignActivity.Description.ShouldContain("Jane Smith");
        assignActivity.Description.ShouldContain("HR");
    }

    // ------------------------------------------------------------------
    // ReturnToStockAsync
    // ------------------------------------------------------------------

    [Fact]
    public async Task ReturnToStockAsync_ShouldClearAssignmentInfoAndSetInStock()
    {
        var asset = await SeedAssetAsync("AST-209911-0001", "Return Test", status: AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "Old User", "old@test.com", "Old Dept");

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.ReturnToStockAsync(asset, "Broken screen", Guid.NewGuid(), "Admin"));

        asset.AssignedToUserId.ShouldBeNull();
        asset.AssignedToUserName.ShouldBeNull();
        asset.AssignedToUserEmail.ShouldBeNull();
        asset.Department.ShouldBeNull();
        asset.AssignedDate.ShouldBeNull();
        asset.Status.ShouldBe(AssetStatus.InStock);
    }

    [Fact]
    public async Task ReturnToStockAsync_ShouldCreateReturnedActivity()
    {
        var asset = await SeedAssetAsync("AST-209911-0002", "Return Activity Test", status: AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "Previous User", "prev@test.com", "Dept");

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.ReturnToStockAsync(asset, "End of contract", Guid.NewGuid(), "Admin"));

        var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
        var allActivities = await WithUnitOfWorkAsync(async () => await activityRepo.GetListAsync());
        var activities = allActivities.Where(a => a.AssetId == asset.Id).ToList();

        var returnActivity = activities.FirstOrDefault(a => a.ActivityType == AssetActivityType.Returned);
        returnActivity.ShouldNotBeNull();
        returnActivity!.Title.ShouldBe("Thu hồi về kho");
        returnActivity.Description.ShouldContain("Previous User");
        returnActivity.Description.ShouldContain("End of contract");
    }

    // ------------------------------------------------------------------
    // ChangeStatusAsync
    // ------------------------------------------------------------------

    [Fact]
    public async Task ChangeStatusAsync_ShouldBeNoOpWhenSameStatus()
    {
        var asset = await SeedAssetAsync("AST-209912-0001", "NoOp Test", status: AssetStatus.InStock);

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.ChangeStatusAsync(asset, AssetStatus.InStock, "Same status", null, null));

        asset.Status.ShouldBe(AssetStatus.InStock);

        // No activity should be created for a no-op.
        var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
        var allActivities = await WithUnitOfWorkAsync(async () => await activityRepo.GetListAsync());
        var count = allActivities.Count(a => a.AssetId == asset.Id);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task ChangeStatusAsync_ToUnderRepair_ShouldCreateSentToRepairActivity()
    {
        var asset = await SeedAssetAsync("AST-209912-0002", "Repair Test", status: AssetStatus.Assigned);

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.ChangeStatusAsync(asset, AssetStatus.UnderRepair, "Faulty keyboard", Guid.NewGuid(), "Admin"));

        asset.Status.ShouldBe(AssetStatus.UnderRepair);

        var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
        var allActivities = await WithUnitOfWorkAsync(async () => await activityRepo.GetListAsync());
        var activities = allActivities.Where(a => a.AssetId == asset.Id).ToList();

        var act = activities.FirstOrDefault(a => a.ActivityType == AssetActivityType.SentToRepair);
        act.ShouldNotBeNull();
        act!.Title.ShouldBe("Gửi đi bảo dưỡng / sửa chữa");
        act.Description.ShouldBe("Faulty keyboard");
    }

    [Fact]
    public async Task ChangeStatusAsync_UnderRepairToInStock_ShouldCreateRepairedActivity()
    {
        var asset = await SeedAssetAsync("AST-209912-0003", "Repaired Test", status: AssetStatus.UnderRepair);

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.ChangeStatusAsync(asset, AssetStatus.InStock, "Fixed and tested", Guid.NewGuid(), "Admin"));

        asset.Status.ShouldBe(AssetStatus.InStock);

        var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
        var allActivities = await WithUnitOfWorkAsync(async () => await activityRepo.GetListAsync());
        var activities = allActivities.Where(a => a.AssetId == asset.Id).ToList();

        var act = activities.FirstOrDefault(a => a.ActivityType == AssetActivityType.Repaired);
        act.ShouldNotBeNull();
        act!.Title.ShouldBe("Hoàn thành sửa chữa - Nhập lại kho");
    }

    [Fact]
    public async Task ChangeStatusAsync_OtherTransition_ShouldCreateStatusChangedActivity()
    {
        var asset = await SeedAssetAsync("AST-209912-0004", "StatusChanged Test", status: AssetStatus.InStock);

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.ChangeStatusAsync(asset, AssetStatus.Reserved, "Reserved for new hire", null, null));

        asset.Status.ShouldBe(AssetStatus.Reserved);

        var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
        var allActivities = await WithUnitOfWorkAsync(async () => await activityRepo.GetListAsync());
        var activities = allActivities.Where(a => a.AssetId == asset.Id).ToList();

        var act = activities.FirstOrDefault(a => a.ActivityType == AssetActivityType.StatusChanged);
        act.ShouldNotBeNull();
        act!.Description.ShouldBe("Reserved for new hire");
    }

    [Fact]
    public async Task ChangeStatusAsync_ToRetired_ShouldClearAssignment()
    {
        var asset = await SeedAssetAsync("AST-209912-0005", "Retire Test", status: AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "User A", "a@test.com", "Dept A");

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.ChangeStatusAsync(asset, AssetStatus.Retired, "End of life", null, null));

        asset.Status.ShouldBe(AssetStatus.Retired);
        asset.AssignedToUserId.ShouldBeNull();
        asset.AssignedToUserName.ShouldBeNull();
        asset.AssignedToUserEmail.ShouldBeNull();
        asset.Department.ShouldBeNull();
        asset.AssignedDate.ShouldBeNull();
    }

    [Fact]
    public async Task ChangeStatusAsync_ToLostStolen_ShouldClearAssignment()
    {
        var asset = await SeedAssetAsync("AST-209912-0006", "Lost Test", status: AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "User B", "b@test.com", "Dept B");

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.ChangeStatusAsync(asset, AssetStatus.LostStolen, "Reported stolen", null, null));

        asset.Status.ShouldBe(AssetStatus.LostStolen);
        asset.AssignedToUserId.ShouldBeNull();
        asset.AssignedToUserName.ShouldBeNull();
    }

    [Fact]
    public async Task ChangeStatusAsync_ToInStock_FromAssigned_ShouldCreateStatusChangedNotRepaired()
    {
        // Transitioning from Assigned to InStock should be StatusChanged, NOT Repaired
        // (Repaired only when transitioning from UnderRepair to InStock).
        var asset = await SeedAssetAsync("AST-209912-0007", "AssignedToInStock", status: AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "User C", "c@test.com", "Dept C");

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.ChangeStatusAsync(asset, AssetStatus.InStock, "Returned", null, null));

        asset.Status.ShouldBe(AssetStatus.InStock);

        var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
        var allActivities = await WithUnitOfWorkAsync(async () => await activityRepo.GetListAsync());
        var activities = allActivities.Where(a => a.AssetId == asset.Id).ToList();

        activities.ShouldNotContain(a => a.ActivityType == AssetActivityType.Repaired);
        activities.ShouldContain(a => a.ActivityType == AssetActivityType.StatusChanged);
    }

    // ------------------------------------------------------------------
    // LogTicketLinkedAsync
    // ------------------------------------------------------------------

    [Fact]
    public async Task LogTicketLinked_ShouldCreateTicketLinkedActivity()
    {
        var asset = await SeedAssetAsync("AST-209913-0001", "Ticket Link Test");
        var ticketId = Guid.NewGuid();
        var performerId = Guid.NewGuid();

        var mgr = BuildAssetManager();
        await WithUnitOfWorkAsync(async () =>
            await mgr.LogTicketLinkedAsync(asset.Id, ticketId, "TK-209901-0001", "Printer not working", performerId, "Admin"));

        var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
        var allActivities = await WithUnitOfWorkAsync(async () => await activityRepo.GetListAsync());
        var activities = allActivities.Where(a => a.AssetId == asset.Id).ToList();

        var act = activities.FirstOrDefault(a => a.ActivityType == AssetActivityType.TicketLinked);
        act.ShouldNotBeNull();
        act!.Title.ShouldBe("Liên kết sự cố: TK-209901-0001");
        act.Description.ShouldBe("Printer not working");
        act.RelatedTicketId.ShouldBe(ticketId);
        act.PerformedByUserId.ShouldBe(performerId);
        act.PerformedByUserName.ShouldBe("Admin");
    }

    // ------------------------------------------------------------------
    // Asset Entity - Pure Unit Tests (no DB)
    // ------------------------------------------------------------------

    [Fact]
    public void Asset_AssignTo_ShouldSetAllFieldsAndStatus()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-UNIT-0001", "Unit Test Asset", AssetType.Laptop);
        var userId = Guid.NewGuid();
        var assignDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        asset.AssignTo(userId, "Test User", "test@test.com", "Engineering", assignDate);

        asset.AssignedToUserId.ShouldBe(userId);
        asset.AssignedToUserName.ShouldBe("Test User");
        asset.AssignedToUserEmail.ShouldBe("test@test.com");
        asset.Department.ShouldBe("Engineering");
        asset.AssignedDate.ShouldBe(assignDate);
        asset.Status.ShouldBe(AssetStatus.Assigned);
    }

    [Fact]
    public void Asset_AssignTo_ShouldUseUtcNowWhenDateNotProvided()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-UNIT-0002", "Default Date Asset", AssetType.Desktop);
        var before = DateTime.UtcNow.AddSeconds(-5);

        asset.AssignTo(Guid.NewGuid(), "User", "u@test.com", "Dept");

        asset.AssignedDate.ShouldNotBeNull();
        (DateTime.UtcNow - asset.AssignedDate!.Value).TotalMinutes.ShouldBeLessThan(1);
        (asset.AssignedDate!.Value - before).TotalSeconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Asset_ReturnToStock_ShouldClearAllAssignmentFields()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-UNIT-0003", "Return Unit", AssetType.Monitor, AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "Some User", "su@test.com", "Some Dept");

        asset.ReturnToStock();

        asset.AssignedToUserId.ShouldBeNull();
        asset.AssignedToUserName.ShouldBeNull();
        asset.AssignedToUserEmail.ShouldBeNull();
        asset.Department.ShouldBeNull();
        asset.AssignedDate.ShouldBeNull();
        asset.Status.ShouldBe(AssetStatus.InStock);
    }

    [Fact]
    public void Asset_ChangeStatus_ToInStock_ShouldClearAssignment()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-UNIT-0004", "Status Unit 1", AssetType.Laptop, AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "User", "u@test.com", "Dept");

        asset.ChangeStatus(AssetStatus.InStock);

        asset.Status.ShouldBe(AssetStatus.InStock);
        asset.AssignedToUserId.ShouldBeNull();
        asset.AssignedToUserName.ShouldBeNull();
        asset.AssignedToUserEmail.ShouldBeNull();
        asset.Department.ShouldBeNull();
        asset.AssignedDate.ShouldBeNull();
    }

    [Fact]
    public void Asset_ChangeStatus_ToRetired_ShouldClearAssignment()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-UNIT-0005", "Status Unit 2", AssetType.Laptop, AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "User", "u@test.com", "Dept");

        asset.ChangeStatus(AssetStatus.Retired);

        asset.Status.ShouldBe(AssetStatus.Retired);
        asset.AssignedToUserId.ShouldBeNull();
        asset.AssignedToUserName.ShouldBeNull();
    }

    [Fact]
    public void Asset_ChangeStatus_ToLostStolen_ShouldClearAssignment()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-UNIT-0006", "Status Unit 3", AssetType.MobileDevice, AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "User", "u@test.com", "Dept");

        asset.ChangeStatus(AssetStatus.LostStolen);

        asset.Status.ShouldBe(AssetStatus.LostStolen);
        asset.AssignedToUserId.ShouldBeNull();
        asset.AssignedToUserName.ShouldBeNull();
    }

    [Fact]
    public void Asset_ChangeStatus_ToUnderRepair_ShouldNotClearAssignment()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-UNIT-0007", "Status Unit 4", AssetType.Laptop, AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "User", "u@test.com", "Dept");

        asset.ChangeStatus(AssetStatus.UnderRepair);

        asset.Status.ShouldBe(AssetStatus.UnderRepair);
        asset.AssignedToUserId.ShouldNotBeNull();
        asset.AssignedToUserName.ShouldBe("User");
    }

    [Fact]
    public void Asset_ChangeStatus_ToReserved_ShouldNotClearAssignment()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-UNIT-0008", "Status Unit 5", AssetType.Laptop, AssetStatus.Assigned);
        asset.AssignTo(Guid.NewGuid(), "User", "u@test.com", "Dept");

        asset.ChangeStatus(AssetStatus.Reserved);

        asset.Status.ShouldBe(AssetStatus.Reserved);
        asset.AssignedToUserId.ShouldNotBeNull();
        asset.AssignedToUserName.ShouldBe("User");
    }

    [Fact]
    public void Asset_Constructor_ShouldValidateTagNotNull()
    {
        var act = () => new Asset(Guid.NewGuid(), "", "No Tag", AssetType.Laptop);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Asset_Constructor_ShouldValidateNameNotNull()
    {
        var act = () => new Asset(Guid.NewGuid(), "AST-UNIT-0009", "  ", AssetType.Laptop);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Asset_UpdateInfo_ShouldUpdateFields()
    {
        var asset = new Asset(Guid.NewGuid(), "AST-UNIT-0010", "Original Name", AssetType.Laptop);

        asset.UpdateInfo("Updated Name", AssetType.Desktop, "NEW-SN", "New Model",
            "New Mfg", "New Loc", new DateTime(2025, 1, 1), new DateTime(2027, 1, 1),
            12345m, "New Specs", "New Notes");

        asset.Name.ShouldBe("Updated Name");
        asset.AssetType.ShouldBe(AssetType.Desktop);
        asset.SerialNumber.ShouldBe("NEW-SN");
        asset.Model.ShouldBe("New Model");
        asset.Manufacturer.ShouldBe("New Mfg");
        asset.Location.ShouldBe("New Loc");
        asset.PurchaseCost.ShouldBe(12345m);
        asset.Specifications.ShouldBe("New Specs");
        asset.Notes.ShouldBe("New Notes");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private AssetManager BuildAssetManager()
    {
        var scope = ServiceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        // Resolve from DI so GuidGenerator is properly set by ABP.
        var mgr = sp.GetRequiredService<AssetManager>();

        // Swap the private _clock field to a FixedClock for deterministic tag generation.
        var fixedClock = new FixedClock(
            sp.GetRequiredService<IOptions<AbpClockOptions>>(),
            sp.GetRequiredService<ICurrentTimezoneProvider>(),
            sp.GetRequiredService<ITimezoneProvider>());

        var clockField = typeof(AssetManager).GetField("_clock",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        clockField.SetValue(mgr, fixedClock);

        return mgr;
    }

    private async Task<Asset> SeedAssetAsync(
        string tag,
        string name,
        AssetStatus status = AssetStatus.InStock,
        string? serial = null)
    {
        var asset = new Asset(
            Guid.NewGuid(),
            tag,
            name,
            AssetType.Laptop,
            status,
            serialNumber: serial);

        var repo = GetRequiredService<IRepository<Asset, Guid>>();
        await WithUnitOfWorkAsync(async () => await repo.InsertAsync(asset, autoSave: true));
        return asset;
    }

    public override void Dispose()
    {
        // Best-effort cleanup of assets and activities created during tests.
        try
        {
            var assetRepo = GetRequiredService<IRepository<Asset, Guid>>();
            var activityRepo = GetRequiredService<IRepository<AssetActivity, Guid>>();
            var dataFilter = GetRequiredService<IDataFilter>();

            using (dataFilter.Disable<ISoftDelete>())
            {
                var allAssets = assetRepo.GetListAsync().GetAwaiter().GetResult();
                // Only clean up test assets (our unique prefixes), not seeded ones.
                var testAssets = allAssets.Where(a =>
                    a.AssetTag.StartsWith("AST-2099") ||
                    a.AssetTag.StartsWith("AST-CUSTOM") ||
                    a.AssetTag.StartsWith("AST-DUP-TAG")).ToList();

                foreach (var asset in testAssets)
                {
                    assetRepo.DeleteAsync(asset, autoSave: true).GetAwaiter().GetResult();
                }

                var allActivities = activityRepo.GetListAsync().GetAwaiter().GetResult();
                var testActivityAssetIds = testAssets.Select(a => a.Id).ToHashSet();
                var testActivities = allActivities.Where(a => testActivityAssetIds.Contains(a.AssetId)).ToList();
                foreach (var act in testActivities)
                {
                    activityRepo.DeleteAsync(act, autoSave: true).GetAwaiter().GetResult();
                }
            }
        }
        catch
        {
            // ignore cleanup failures
        }
        base.Dispose();
    }

    private sealed class FixedClock : Clock
    {
        private readonly AbpClockOptions _options;
        private readonly ICurrentTimezoneProvider _currentTimezoneProvider;
        private readonly ITimezoneProvider _timezoneProvider;

        public FixedClock(
            IOptions<AbpClockOptions> options,
            ICurrentTimezoneProvider currentTimezoneProvider,
            ITimezoneProvider timezoneProvider)
            : base(options, currentTimezoneProvider, timezoneProvider)
        {
            _options = options.Value;
            _currentTimezoneProvider = currentTimezoneProvider;
            _timezoneProvider = timezoneProvider;
        }

        public override DateTime Now => _fixedNow;
    }
}
