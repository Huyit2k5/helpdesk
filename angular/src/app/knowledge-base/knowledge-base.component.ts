import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PermissionService } from '@abp/ng.core';
import { KnowledgeArticleService, KnowledgeArticleDto } from '../proxy/knowledge-base';
import { CategoryService, CategoryLookupDto } from '../proxy/categories';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-knowledge-base',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './knowledge-base.component.html',
  styleUrls: ['./knowledge-base.component.scss']
})
export class KnowledgeBaseComponent implements OnInit {
  private articleService = inject(KnowledgeArticleService);
  private categoryService = inject(CategoryService);
  private permissionService = inject(PermissionService);

  articles: KnowledgeArticleDto[] = [];
  popularArticles: KnowledgeArticleDto[] = [];
  categories: CategoryLookupDto[] = [];
  selectedCategoryId: string | null = null;
  selectedTag: string | null = null;
  searchTerm = '';
  isLoading = false;
  totalCount = 0;

  get canManage(): boolean {
    return this.permissionService.getGrantedPolicy('Helpdesk.KnowledgeBase.Manage') ||
           this.permissionService.getGrantedPolicy('Helpdesk.KnowledgeBase.Create');
  }

  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    this.loadCategories();
    this.loadPopularArticles();
    this.loadArticles();

    this.searchSubject.pipe(
      debounceTime(350),
      distinctUntilChanged()
    ).subscribe(term => {
      this.searchTerm = term;
      this.loadArticles();
    });
  }

  loadCategories(): void {
    this.categoryService.getLookup().subscribe(res => {
      this.categories = res.items || [];
    });
  }

  loadPopularArticles(): void {
    this.articleService.getPopularArticles(5).subscribe(res => {
      this.popularArticles = res || [];
    });
  }

  loadArticles(): void {
    this.isLoading = true;
    this.articleService.getList({
      filter: this.searchTerm || undefined,
      categoryId: this.selectedCategoryId || undefined,
      tag: this.selectedTag || undefined,
      isPublished: true,
      maxResultCount: 20,
      skipCount: 0
    }).subscribe({
      next: (res) => {
        this.articles = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onSearchChange(val: string): void {
    this.searchSubject.next(val);
  }

  selectCategory(catId: string | null): void {
    this.selectedCategoryId = catId;
    this.selectedTag = null;
    this.loadArticles();
  }

  selectTag(tag: string | null): void {
    this.selectedTag = tag;
    this.loadArticles();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategoryId = null;
    this.selectedTag = null;
    this.loadArticles();
  }
}
