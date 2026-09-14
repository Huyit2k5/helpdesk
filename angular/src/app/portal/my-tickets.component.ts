import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CustomerPortalService, CustomerTicketDto, CreateCustomerTicketDto } from '../proxy/customer-portal';
import { KnowledgeArticleService, KnowledgeArticleSuggestionDto } from '../proxy/knowledge-base';
import { CategoryService, CategoryLookupDto } from '../proxy/categories';
import { PriorityService, PriorityDto } from '../proxy/priorities';
import { AssetDto } from '../proxy/assets/models';
import { Subject, forkJoin } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-my-tickets',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './my-tickets.component.html',
  styleUrls: ['./my-tickets.component.scss']
})
export class MyTicketsComponent implements OnInit {
  private portalService = inject(CustomerPortalService);
  private kbService = inject(KnowledgeArticleService);
  private categoryService = inject(CategoryService);
  private priorityService = inject(PriorityService);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  tickets: CustomerTicketDto[] = [];
  categories: CategoryLookupDto[] = [];
  priorities: PriorityDto[] = [];
  myAssets: AssetDto[] = [];
  totalCount = 0;
  isLoading = false;

  // Filter state
  activeTab: 'all' | 'open' | 'closed' = 'open';
  searchTerm = '';

  // Create Ticket Modal State
  isCreateModalOpen = false;
  createForm!: FormGroup;
  isSubmitting = false;

  // Real-time Deflection Suggestions
  suggestions: KnowledgeArticleSuggestionDto[] = [];
  isSearchingSuggestions = false;
  private titleSubject = new Subject<string>();

  ngOnInit(): void {
    this.buildForm();
    this.loadCategories();
    this.loadPriorities();
    this.loadMyAssets();
    this.loadTickets();

    // Deflection suggestion debouncer
    this.titleSubject.pipe(
      debounceTime(350),
      distinctUntilChanged()
    ).subscribe(title => {
      this.searchSuggestions(title);
    });

    // Check if opened with ?create=true
    this.route.queryParams.subscribe(params => {
      if (params['create'] === 'true') {
        const assetId = params['assetId'];
        const assetTag = params['assetTag'];
        const assetName = params['assetName'];
        this.openCreateModal(assetId, assetTag, assetName);
      }
    });
  }

  buildForm(): void {
    this.createForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      categoryId: ['', [Validators.required]],
      priorityId: [''],
      assetId: [''],
      description: ['', [Validators.required]]
    });
  }

  loadCategories(): void {
    this.categoryService.getLookup().subscribe(res => {
      this.categories = res.items || [];
      this.cdr.detectChanges();
    });
  }

  loadPriorities(): void {
    this.priorityService.getList({ maxResultCount: 50, skipCount: 0 }).subscribe(res => {
      this.priorities = res.items || [];
      this.cdr.detectChanges();
    });
  }

  loadMyAssets(): void {
    this.portalService.getMyAssets().subscribe({
      next: (res) => {
        this.myAssets = res || [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  setTab(tab: 'all' | 'open' | 'closed'): void {
    this.activeTab = tab;
    this.loadTickets();
  }

  loadTickets(): void {
    this.isLoading = true;
    let isClosedParam: boolean | undefined = undefined;
    if (this.activeTab === 'open') isClosedParam = false;
    if (this.activeTab === 'closed') isClosedParam = true;

    this.portalService.getMyTickets({
      filter: this.searchTerm || undefined,
      isClosed: isClosedParam,
      skipCount: 0,
      maxResultCount: 100
    }).subscribe({
      next: (res) => {
        this.tickets = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onTabChange(tab: 'all' | 'open' | 'closed'): void {
    this.activeTab = tab;
    this.loadTickets();
  }

  onSearch(): void {
    this.loadTickets();
  }

  onTitleChange(title: string): void {
    this.titleSubject.next(title);
  }

  searchSuggestions(query: string): void {
    if (!query || query.trim().length < 3) {
      this.suggestions = [];
      this.cdr.detectChanges();
      return;
    }

    this.isSearchingSuggestions = true;
    this.kbService.getSuggestions(query.trim(), 4).subscribe({
      next: (res) => {
        this.suggestions = res || [];
        this.isSearchingSuggestions = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isSearchingSuggestions = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectedFiles: File[] = [];

  openCreateModal(assetId?: string, assetTag?: string, assetName?: string): void {
    this.createForm.reset({
      title: assetTag ? `[Sự cố thiết bị ${assetTag}] ` : '',
      categoryId: this.categories.length > 0 ? this.categories[0].id : '',
      priorityId: '',
      assetId: assetId || '',
      description: assetName ? `Thiết bị liên quan: [${assetTag}] ${assetName}\n\nMô tả chi tiết sự cố gặp phải:\n- Hiện tượng:\n- Thời điểm xảy ra:` : ''
    });
    this.suggestions = [];
    this.selectedFiles = [];
    this.isCreateModalOpen = true;
    this.cdr.detectChanges();
  }

  closeCreateModal(): void {
    this.isCreateModalOpen = false;
    this.selectedFiles = [];
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const newFiles = Array.from(input.files);
      const validFiles = newFiles.filter(f => {
        if (f.size > 10 * 1024 * 1024) {
          alert(`Tệp "${f.name}" vượt quá dung lượng tối đa 10 MB.`);
          return false;
        }
        return true;
      });
      this.selectedFiles.push(...validFiles);
      this.cdr.detectChanges();
    }
  }

  removeSelectedFile(index: number): void {
    this.selectedFiles.splice(index, 1);
    this.cdr.detectChanges();
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  resolveWithDeflection(): void {
    this.closeCreateModal();
    alert('Tuyệt vời! Chúng tôi rất vui vì bài viết hướng dẫn đã giúp giải quyết vấn đề của bạn.');
  }

  submitTicket(): void {
    if (this.createForm.invalid || this.isSubmitting) return;

    this.isSubmitting = true;
    const val: CreateCustomerTicketDto = this.createForm.value;

    this.portalService.createMyTicket(val).subscribe({
      next: (createdTicket) => {
        if (this.selectedFiles.length > 0 && createdTicket && createdTicket.id) {
          const uploads = this.selectedFiles.map(f =>
            this.portalService.uploadMyAttachment(createdTicket.id!, f)
          );
          forkJoin(uploads).subscribe({
            next: () => {
              this.finishSubmit();
            },
            error: () => {
              this.finishSubmit();
            }
          });
        } else {
          this.finishSubmit();
        }
      },
      error: () => {
        this.isSubmitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  private finishSubmit(): void {
    this.isSubmitting = false;
    this.closeCreateModal();
    this.loadTickets();
    this.cdr.detectChanges();
  }
}
