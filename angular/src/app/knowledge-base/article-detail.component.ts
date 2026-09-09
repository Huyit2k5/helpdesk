import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { KnowledgeArticleService, KnowledgeArticleDto } from '../proxy/knowledge-base';

@Component({
  selector: 'app-article-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './article-detail.component.html',
  styleUrls: ['./article-detail.component.scss']
})
export class ArticleDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private articleService = inject(KnowledgeArticleService);
  private cdr = inject(ChangeDetectorRef);

  article: KnowledgeArticleDto | null = null;
  isLoading = true;
  hasVoted = false;
  voteSuccessMessage: string | null = null;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const slug = params.get('slug');
      if (slug) {
        this.loadArticle(slug);
      }
    });
  }

  loadArticle(slug: string): void {
    this.isLoading = true;
    this.articleService.getBySlug(slug).subscribe({
      next: (res) => {
        this.article = res;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  vote(isHelpful: boolean): void {
    if (!this.article || !this.article.id || this.hasVoted) return;

    this.articleService.vote(this.article.id, isHelpful).subscribe({
      next: () => {
        this.hasVoted = true;
        if (isHelpful) {
          this.article!.helpfulCount++;
          this.voteSuccessMessage = 'Cảm ơn bạn! Phản hồi của bạn giúp chúng tôi cải thiện chất lượng hỗ trợ.';
        } else {
          this.article!.notHelpfulCount++;
          this.voteSuccessMessage = 'Rất tiếc bài viết chưa hỗ trợ được bạn. Bạn có thể gửi yêu cầu hỗ trợ trực tiếp bên dưới.';
        }
        this.cdr.detectChanges();
      }
    });
  }

  formatContent(content?: string): string {
    if (!content) return '';
    // Basic Markdown formatting for headings, lists, bold, and code blocks
    let html = content
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');

    // Headings
    html = html.replace(/^### (.*$)/gim, '<h4 class="fw-bold mt-4 mb-2 text-dark">$1</h4>');
    html = html.replace(/^## (.*$)/gim, '<h3 class="fw-bold mt-4 mb-2 text-dark border-bottom pb-2">$1</h3>');
    html = html.replace(/^# (.*$)/gim, '<h2 class="fw-bold mt-4 mb-3 text-dark">$1</h2>');

    // Bold & italic
    html = html.replace(/\*\*(.*?)\*\*/gim, '<strong>$1</strong>');
    html = html.replace(/\*(.*?)\*/gim, '<em>$1</em>');

    // Code tags
    html = html.replace(/`([^`]+)`/gim, '<code class="bg-light px-2 py-1 rounded text-primary fw-semibold">$1</code>');

    // Ordered/unordered lists
    html = html.replace(/^\s*\d+\.\s+(.*$)/gim, '<li class="mb-1">$1</li>');
    html = html.replace(/^\s*-\s+(.*$)/gim, '<li class="mb-1">$1</li>');

    // Wrap list items
    html = html.replace(/(<li.*<\/li>)/gms, '<ul class="ps-3 my-2">$1</ul>');

    // Paragraphs
    html = html.replace(/\n\n+/g, '<br/><br/>');

    return html;
  }
}
