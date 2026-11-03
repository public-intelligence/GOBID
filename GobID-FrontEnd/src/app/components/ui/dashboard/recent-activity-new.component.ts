import { Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faInfoCircle,
  faCheckCircle,
  faExclamationTriangle,
  faTimesCircle,
} from '@fortawesome/free-solid-svg-icons';
import { DashboardActivity } from '../../../models';

@Component({
  selector: 'app-recent-activity',
  imports: [DatePipe, FontAwesomeModule],
  template: `
    <div
      class="h-full rounded-xl border border-gray-100 bg-white p-6 shadow-sm"
    >
      <div class="mb-6 border-b border-gray-100 pb-4">
        <h3 class="text-lg font-semibold text-gray-900">Actividad Reciente</h3>
      </div>

      <div class="custom-scrollbar max-h-96 overflow-y-auto">
        @if (activities.length > 0) {
          @for (activity of activities; track activity.id) {
            <div
              class="flex gap-4 rounded-lg border-b border-gray-50 p-4 transition-colors duration-200 last:border-b-0 hover:bg-gray-50"
              [class]="getActivityClass(activity.type)"
            >
              <div class="flex-shrink-0">
                <div
                  class="flex h-8 w-8 items-center justify-center rounded-full text-sm"
                  [class]="getIconContainerClass(activity.type)"
                >
                  <fa-icon [icon]="getIcon(activity.type)"></fa-icon>
                </div>
              </div>

              <div class="min-w-0 flex-1">
                <div class="mb-2">
                  <h4 class="text-sm leading-5 font-semibold text-gray-900">
                    {{ activity.title }}
                  </h4>
                  <p class="mt-1 text-sm leading-5 text-gray-600">
                    {{ activity.description }}
                  </p>
                </div>

                <div class="flex items-center justify-between text-xs">
                  @if (activity.user) {
                    <span class="font-medium text-gray-700">{{
                      activity.user
                    }}</span>
                  }
                  <span class="ml-auto whitespace-nowrap text-gray-500">{{
                    activity.timestamp | date: 'short'
                  }}</span>
                </div>
              </div>
            </div>
          }
        } @else {
          <div
            class="flex flex-col items-center justify-center py-12 text-center"
          >
            <fa-icon
              [icon]="faInfoCircle"
              class="mb-3 text-4xl text-gray-400"
            ></fa-icon>
            <span class="text-gray-500">No hay actividades recientes</span>
          </div>
        }
      </div>
    </div>
  `,
  styles: [
    `
      .custom-scrollbar::-webkit-scrollbar {
        width: 4px;
      }

      .custom-scrollbar::-webkit-scrollbar-track {
        background: #f1f5f9;
        border-radius: 4px;
      }

      .custom-scrollbar::-webkit-scrollbar-thumb {
        background: #cbd5e1;
        border-radius: 4px;
      }

      .custom-scrollbar::-webkit-scrollbar-thumb:hover {
        background: #94a3b8;
      }
    `,
  ],
})
export class RecentActivityComponent {
  @Input() activities: DashboardActivity[] = [];

  // Icons
  protected faInfoCircle = faInfoCircle;
  private faCheckCircle = faCheckCircle;
  private faExclamationTriangle = faExclamationTriangle;
  private faTimesCircle = faTimesCircle;

  getActivityClass(_type: string): string {
    // Base classes for the activity item
    return '';
  }

  getIconContainerClass(type: string): string {
    switch (type) {
      case 'success':
        return 'bg-green-100 text-green-600';
      case 'warning':
        return 'bg-yellow-100 text-yellow-600';
      case 'error':
        return 'bg-red-100 text-red-600';
      default:
        return 'bg-blue-100 text-blue-600';
    }
  }

  getIcon(type: string) {
    switch (type) {
      case 'success':
        return this.faCheckCircle;
      case 'warning':
        return this.faExclamationTriangle;
      case 'error':
        return this.faTimesCircle;
      default:
        return this.faInfoCircle;
    }
  }
}
