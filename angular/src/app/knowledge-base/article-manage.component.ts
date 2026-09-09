import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { KnowledgeArticleService, KnowledgeArticleDto, CreateUpdateKnowledgeArticleDto } from '../proxy/knowledge-base';
import { CategoryService, CategoryLookupDto } from '../proxy/categories';

@Component({
  selector: 'app-article-manage',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './article-manage.component.html',
  styleUrls: ['./article-manage.component.scss']
})
export class ArticleManageComponent implements OnInit {
  private articleService = inject(KnowledgeArticleService);
  private categoryService = inject(CategoryService);
  private confirmationService = inject(ConfirmationService);
  private fb = inject(FormBuilder);

  articles: KnowledgeArticleDto[] = [];
  categories: CategoryLookupDto[] = [];
  isLoading = false;
  totalCount = 0;
  searchTerm = '';

  isModalOpen = false;
  isEditMode = false;
  selectedArticleId: string | null = null;
  form!: FormGroup;

  ngOnInit(): void {
    this.buildForm();
    this.loadCategories();
    this.loadArticles();
  }

  buildForm(): void {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      categoryId: ['', [Validators.required]],
      summary: ['', [Validators.maxLength(1000)]],
      content: ['', [Validators.required]],
      tags: ['', [Validators.maxLength(500)]],
      isPublished: [true]
    });
  }

  loadCategories(): void {
    this.categoryService.getLookup().subscribe(res => {
      this.categories = res.items || [];
    });
  }

  loadArticles(): void {
    this.isLoading = true;
    this.articleService.getList({
      filter: this.searchTerm || undefined,
      maxResultCount: 50,
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

  openCreateModal(): void {
    this.isEditMode = false;
    this.selectedArticleId = null;
    this.form.reset({
      title: '',
      categoryId: this.categories.length > 0 ? this.categories[0].id : '',
      summary: '',
      content: '',
      tags: '',
      isPublished: true
    });
    this.isModalOpen = true;
  }

  openEditModal(article: KnowledgeArticleDto): void {
    this.isEditMode = true;
    this.selectedArticleId = article.id || null;
    this.form.patchValue({
      title: article.title,
      categoryId: article.categoryId,
      summary: article.summary,
      content: article.content,
      tags: article.tags,
      isPublished: article.isPublished
    });
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  save(): void {
    if (this.form.invalid) return;

    const val: CreateUpdateKnowledgeArticleDto = this.form.value;

    if (this.isEditMode && this.selectedArticleId) {
      this.articleService.update(this.selectedArticleId, val).subscribe(() => {
        this.closeModal();
        this.loadArticles();
      });
    } else {
      this.articleService.create(val).subscribe(() => {
        this.closeModal();
        this.loadArticles();
      });
    }
  }

  togglePublish(article: KnowledgeArticleDto): void {
    if (!article.id) return;
    const input: CreateUpdateKnowledgeArticleDto = {
      title: article.title,
      categoryId: article.categoryId,
      summary: article.summary,
      content: article.content,
      tags: article.tags,
      isPublished: !article.isPublished
    };

    this.articleService.update(article.id, input).subscribe(() => {
      article.isPublished = !article.isPublished;
    });
  }

  deleteArticle(article: KnowledgeArticleDto): void {
    if (!article.id) return;
    this.confirmationService.warn(
      `Bạn có chắc chắn muốn xóa bài viết "${article.title}" không?`,
      'Xác nhận xóa bài viết'
    ).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm) {
        this.articleService.delete(article.id!).subscribe(() => {
          this.loadArticles();
        });
      }
    });
  }
}
