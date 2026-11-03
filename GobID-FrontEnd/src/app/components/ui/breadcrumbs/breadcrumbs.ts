import { NgClass, TitleCasePipe } from '@angular/common';
import { Component, inject, Input } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faChevronRight } from '@fortawesome/free-solid-svg-icons';
import { Breadcrumb } from '../../../models/breadcrumbs';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-breadcrumbs',
  imports: [TitleCasePipe, FontAwesomeModule, NgClass, RouterModule],
  templateUrl: './breadcrumbs.html',
})
export class BreadcrumbsComponent {
  @Input() icon: IconProp | string = '';
  @Input() breadcrumbs!: Breadcrumb[];

  faChevronRight = faChevronRight;

  private router = inject(Router);

  navigateTo(breadcrumb: Breadcrumb): void {
    if (!breadcrumb.path) return;
    this.router.navigate([breadcrumb.path]);
  }
}
