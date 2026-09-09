import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CustomerPortalService, CustomerTicketDto, CreateCustomerTicketDto } from '../proxy/customer-portal';
import { KnowledgeArticleService, KnowledgeArticleSuggestionDto } from '../proxy/knowledge-base';
import { CategoryService, CategoryLookupDto } from '../proxy/categories';
import { PriorityService, PriorityDto } from '../proxy/priorities';
import { Subject } from 'rxjs';
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

  tickets: CustomerTicketDto[] = [];
  categories: CategoryLookupDto[] = [];
  priorities: PriorityDto[] = [];
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
        this.openCreateModal();
      }
    });
  }

  buildForm(): void {
    this.createForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      categoryId: ['', [Validators.required]],
      priorityId: [''],
      description: ['', [Validators.required]]
    });
  }

  loadCategories(): void {
    this.categoryService.getLookup().subscribe(res => {
      this.categories = res.items || [];
    });
  }

  loadPriorities(): void {
    this.priorityService.getList({ maxResultCount: 50, skipCount: 0 }).subscribe(res => {
      this.priorities = res.items || [];
    });
  }

  loadTickets(): void {
    this.isLoading = true;
    let isClosedParam: boolean | undefined = undefined;
    if (this.activeTab === 'open') isClosedParam = false;
    if (this.activeTab === 'closed') isClosedParam = true;

    this.portalService.getMyTickets({
      filter: this.searchTerm || undefined,
      isClosed: isClosedParam,
      maxResultCount: 50,
      skipCount: 0
    }).subscribe({
      next: (res) => {
        this.tickets = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  setTab(tab: 'all' | 'open' | 'closed'): void {
    this.activeTab = tab;
    this.loadTickets();
  }

  onTitleChange(val: string): void {
    this.titleSubject.next(val);
  }

  searchSuggestions(title: string): void {
    if (!title || title.trim().length < 3) {
      this.suggestions = [];
      return;
    }

    this.isSearchingSuggestions = true;
    this.kbService.getSuggestions(title.trim(), 3).subscribe({
      next: (res) => {
        this.suggestions = res || [];
        this.isSearchingSuggestions = false;
      },
      error: () => {
        this.suggestions = [];
        this.isSearchingSuggestions = false;
      }
    });
  }

  openCreateModal(): void {
    this.createForm.reset({
      title: '',
      categoryId: this.categories.length > 0 ? this.categories[0].id : '',
      priorityId: '',
      description: ''
    });
    this.suggestions = [];
    this.isCreateModalOpen = true;
  }

  closeCreateModal(): void {
    this.isCreateModalOpen = false;
  }

  resolveWithDeflection(): void {
    // Customer found an answer from suggested articles!
    this.closeCreateModal();
    alert('Tuyệt vời! Chúng tôi rất vui vì bài viết hướng dẫn đã giúp giải quyết vấn đề của bạn.');
  }

  submitTicket(): void {
    if (this.createForm.invalid || this.isSubmitting) return;

    this.isSubmitting = true;
    const val: CreateCustomerTicketDto = this.createForm.value;

    this.portalService.createMyTicket(val).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeCreateModal();
        this.loadTickets();
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }
}
