import { Component, Input } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faArrowUp,
  faArrowDown,
  faMinus,
  faUsers,
  faBuilding,
  faDesktop,
  faCheckCircle,
  faExclamationTriangle,
  faInfoCircle,
} from '@fortawesome/free-solid-svg-icons';
import { DashboardCard } from '../../../models';

@Component({
  selector: 'app-dashboard-card',
  imports: [FontAwesomeModule],
  template: `
    <div
      class="h-full rounded-xl border border-gray-100 bg-white p-4 shadow-sm transition-all duration-200 hover:-translate-y-1 hover:shadow-md"
      [class]="getCardBorderClass()"
    >
      <div class="mb-2 flex items-center justify-between">
        <h3 class="text-sm font-semibold tracking-wide text-gray-600 uppercase">
          {{ card.title }}
        </h3>
        @if (card.icon) {
          <fa-icon
            [icon]="getIcon(card.icon)"
            [class]="getIconClass()"
          ></fa-icon>
        }
      </div>

      <div class="space-y-1">
        <div class="text-2xl font-bold text-gray-900">{{ card.value }}</div>

        @if (card.subtitle) {
          <div class="text-sm text-gray-500">{{ card.subtitle }}</div>
        }

        @if (card.trend && card.trendValue) {
          <div
            class="flex items-center gap-1 text-sm font-medium"
            [class]="getTrendClass()"
          >
            <fa-icon
              [icon]="getTrendIcon(card.trend)"
              class="text-xs"
            ></fa-icon>
            <span>{{ card.trendValue }}</span>
          </div>
        }
      </div>
    </div>
  `,
})
export class DashboardCardComponent {
  @Input() card!: DashboardCard;

  // Icons
  private faArrowUp = faArrowUp;
  private faArrowDown = faArrowDown;
  private faMinus = faMinus;
  private faUsers = faUsers;
  private faBuilding = faBuilding;
  private faDesktop = faDesktop;
  private faCheckCircle = faCheckCircle;
  private faExclamationTriangle = faExclamationTriangle;
  private faInfoCircle = faInfoCircle;

  getCardBorderClass(): string {
    const color = this.card.color || 'primary';
    const borderColors = {
      primary: 'border-l-4 border-l-blue-500',
      success: 'border-l-4 border-l-green-500',
      warning: 'border-l-4 border-l-yellow-500',
      danger: 'border-l-4 border-l-red-500',
      info: 'border-l-4 border-l-cyan-500',
    };
    return (
      borderColors[color as keyof typeof borderColors] || borderColors.primary
    );
  }

  getIconClass(): string {
    const color = this.card.color || 'primary';
    const iconColors = {
      primary: 'text-blue-500 text-xl',
      success: 'text-green-500 text-xl',
      warning: 'text-yellow-500 text-xl',
      danger: 'text-red-500 text-xl',
      info: 'text-cyan-500 text-xl',
    };
    return iconColors[color as keyof typeof iconColors] || iconColors.primary;
  }

  getTrendClass(): string {
    switch (this.card.trend) {
      case 'up':
        return 'text-green-600';
      case 'down':
        return 'text-red-600';
      default:
        return 'text-gray-600';
    }
  }

  getTrendIcon(trend: string) {
    switch (trend) {
      case 'up':
        return this.faArrowUp;
      case 'down':
        return this.faArrowDown;
      default:
        return this.faMinus;
    }
  }

  getIcon(iconName: string) {
    switch (iconName) {
      case 'users':
        return this.faUsers;
      case 'building':
        return this.faBuilding;
      case 'desktop':
        return this.faDesktop;
      case 'check':
        return this.faCheckCircle;
      case 'warning':
        return this.faExclamationTriangle;
      case 'info':
        return this.faInfoCircle;
      default:
        return this.faInfoCircle;
    }
  }
}
