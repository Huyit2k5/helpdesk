import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService, PagedResultDto, LocalizationPipe } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { CategoryDto, CategoryGetListInput, CreateUpdateCategoryDto, CategoryLookupDto } from '../../proxy/categories/models';
import { CategoryService } from '../../proxy/categories/category.service';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule, LocalizationPipe],
  providers: [ListService],
  templateUrl: './categories.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoriesComponent implements OnInit {
  private categoryService = inject(CategoryService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<CategoryGetListInput>);

  items: CategoryDto[] = [];
  totalCount = 0;
  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';
  parentCategories: CategoryLookupDto[] = [];

  form: FormGroup = this.fb.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(500)],
    parentId: [null],
    order: [0, Validators.required],
    isActive: [true],
  });

  ngOnInit(): void {
    this.loadParentCategories();
    const streamCreator = (query: CategoryGetListInput) =>
      this.categoryService.getList({ ...query, filter: this.filterText });

    this.list.hookToQuery(streamCreator).subscribe(response => {
      this.items = response.items ?? [];
      this.totalCount = response.totalCount ?? 0;
      this.cdr.markForCheck();
    });
  }

  loadParentCategories(): void {
    this.categoryService.getLookup().subscribe(data => {
      this.parentCategories = data.items ?? [];
      this.cdr.markForCheck();
    });
  }

  search(): void {
    this.list.get();
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedId = undefined;
    this.form.reset({ isActive: true, order: 0 });
    this.isModalOpen = true;
  }

  openEditModal(item: CategoryDto): void {
    this.isEditing = true;
    this.selectedId = item.id;
    this.form.patchValue({
      code: item.code,
      name: item.name,
      description: item.description,
      parentId: item.parentId || null,
      order: item.order ?? 0,
      isActive: item.isActive,
    });
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  save(): void {
    if (this.form.invalid) return;
    const input: CreateUpdateCategoryDto = this.form.value;
    const request = this.isEditing
      ? this.categoryService.update(this.selectedId!, input)
      : this.categoryService.create(input);

    request.subscribe(() => {
      this.isModalOpen = false;
      this.list.get();
    });
  }

  delete(item: CategoryDto): void {
    this.confirmation.warn('::AreYouSureToDelete', '::AreYouSure', {
      messageLocalizationParams: [item.name ?? ''],
    }).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm && item.id) {
        this.categoryService.delete(item.id).subscribe(() => this.list.get());
      }
    });
  }
}
