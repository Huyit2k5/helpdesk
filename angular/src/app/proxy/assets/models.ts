import type { CreationAuditedEntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export enum AssetType {
  Laptop = 1,
  Desktop = 2,
  Monitor = 3,
  NetworkDevice = 4,
  PrinterPeripheral = 5,
  ServerStorage = 6,
  SoftwareLicense = 7,
  MobileDevice = 8,
  Other = 9,
}

export enum AssetStatus {
  InStock = 1,
  Assigned = 2,
  UnderRepair = 3,
  Reserved = 4,
  Retired = 5,
  LostStolen = 6,
}

export enum AssetActivityType {
  Created = 1,
  Assigned = 2,
  Returned = 3,
  StatusChanged = 4,
  SentToRepair = 5,
  Repaired = 6,
  TicketLinked = 7,
  NoteAdded = 8,
  HandoverConfirmed = 9,
}

export interface AssetDto extends FullAuditedEntityDto<string> {
  assetTag: string;
  name: string;
  assetType: AssetType;
  assetTypeName: string;
  status: AssetStatus;
  statusName: string;
  serialNumber?: string | null;
  model?: string | null;
  manufacturer?: string | null;
  location?: string | null;
  purchaseDate?: string | null;
  warrantyExpiryDate?: string | null;
  purchaseCost?: number | null;
  assignedToUserId?: string | null;
  assignedToUserName?: string | null;
  assignedToUserEmail?: string | null;
  department?: string | null;
  assignedDate?: string | null;
  specifications?: string | null;
  notes?: string | null;
  isHandoverConfirmed: boolean;
  handoverConfirmedDate?: string | null;
  handoverNotes?: string | null;
  openTicketCount: number;
}

export interface AssetDetailDto extends AssetDto {
  activities: AssetActivityDto[];
  tickets: AssetTicketDto[];
}

export interface AssetActivityDto extends CreationAuditedEntityDto<string> {
  assetId: string;
  activityType: AssetActivityType;
  activityTypeName: string;
  title: string;
  description?: string | null;
  performedByUserId?: string | null;
  performedByUserName?: string | null;
  relatedTicketId?: string | null;
}

export interface AssetTicketDto {
  id: string;
  ticketNumber: string;
  title: string;
  statusName: string;
  priorityName: string;
  requesterName: string;
  creationTime: string;
}

export interface GetAssetsInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  assetType?: AssetType | null;
  status?: AssetStatus | null;
  department?: string | null;
  assignedToUserId?: string | null;
  warrantyExpiringSoon?: boolean | null;
}

export interface CreateAssetDto {
  assetTag?: string | null;
  name: string;
  assetType: AssetType;
  status: AssetStatus;
  serialNumber?: string | null;
  model?: string | null;
  manufacturer?: string | null;
  location?: string | null;
  purchaseDate?: string | null;
  warrantyExpiryDate?: string | null;
  purchaseCost?: number | null;
  specifications?: string | null;
  notes?: string | null;
}

export interface UpdateAssetDto {
  name: string;
  assetType: AssetType;
  serialNumber?: string | null;
  model?: string | null;
  manufacturer?: string | null;
  location?: string | null;
  purchaseDate?: string | null;
  warrantyExpiryDate?: string | null;
  purchaseCost?: number | null;
  specifications?: string | null;
  notes?: string | null;
}

export interface AssignAssetDto {
  userId?: string | null;
  userName: string;
  userEmail?: string | null;
  department?: string | null;
  notes?: string | null;
}

export interface ReturnAssetDto {
  notes?: string | null;
}

export interface ChangeAssetStatusDto {
  status: AssetStatus;
  reason?: string | null;
}

export interface AssetTypeStockDto {
  assetType: AssetType;
  assetTypeName: string;
  totalCount: number;
  inStockCount: number;
  assignedCount: number;
}

export interface AssetKpiDto {
  totalAssets: number;
  inStockCount: number;
  assignedCount: number;
  underRepairCount: number;
  warrantyExpiringSoonCount: number;
  stockByType?: AssetTypeStockDto[];
}

export interface ConfirmAssetHandoverDto {
  notes?: string | null;
}

export interface AssetReceiptDto {
  assetId: string;
  receiptNumber: string;
  receiptType: string;
  title: string;
  generatedDate: string;
  giverName: string;
  giverRole: string;
  giverEmail?: string | null;
  receiverName: string;
  receiverEmail?: string | null;
  receiverDepartment?: string | null;
  assetTag: string;
  assetName: string;
  assetTypeName: string;
  model?: string | null;
  serialNumber?: string | null;
  manufacturer?: string | null;
  specifications?: string | null;
  location?: string | null;
  assignedDate?: string | null;
  warrantyExpiryDate?: string | null;
  condition: string;
  accessories: string;
  notes?: string | null;
  isConfirmed: boolean;
  confirmedDate?: string | null;
}

export type AssetLookupDto = AssetDto;
