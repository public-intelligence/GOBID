import { Component, Input, OnInit, signal } from '@angular/core';
import { DashboardChart } from '../../../models';

@Component({
  selector: 'app-dashboard-chart',
  imports: [],
  template: `
    <div
      class="h-full rounded-xl border border-gray-100 bg-white p-6 shadow-sm"
    >
      <div class="mb-5 border-b border-gray-100 pb-4">
        <h3 class="text-lg font-semibold text-gray-900">{{ chart.title }}</h3>
        @if (chart.description) {
          <p class="mt-1 text-sm text-gray-600">{{ chart.description }}</p>
        }
      </div>

      <div class="h-80">
        <!-- Bar Chart -->
        @if (chart.type === 'bar') {
          <div class="h-full">
            <div class="flex h-64 items-end justify-between gap-3 px-4 py-5">
              @for (item of chart.data; track item.label; let i = $index) {
                <div class="flex flex-1 flex-col items-center gap-2">
                  <div
                    class="min-h-1 w-full rounded-t-md transition-all duration-300 hover:opacity-80"
                    [style.height.%]="getBarHeight(item.value)"
                    [style.background-color]="item.color || getDefaultColor(i)"
                  ></div>
                  <span class="text-center text-xs text-gray-600">{{
                    item.label
                  }}</span>
                  <span class="text-sm font-semibold text-gray-900">{{
                    item.value
                  }}</span>
                </div>
              }
            </div>
          </div>
        }

        <!-- Pie/Doughnut Chart -->
        @if (chart.type === 'pie' || chart.type === 'doughnut') {
          <div class="flex h-full flex-col gap-5">
            <div class="relative flex flex-1 items-center justify-center">
              <svg viewBox="0 0 200 200" class="h-48 w-48">
                @for (segment of pieSegments(); track $index; let i = $index) {
                  <circle
                    [attr.cx]="100"
                    [attr.cy]="100"
                    [attr.r]="chart.type === 'doughnut' ? 60 : 80"
                    fill="transparent"
                    [attr.stroke]="segment.color"
                    [attr.stroke-width]="chart.type === 'doughnut' ? 20 : 80"
                    [attr.stroke-dasharray]="segment.dashArray"
                    [attr.stroke-dashoffset]="segment.dashOffset"
                    transform="rotate(-90 100 100)"
                    class="transition-opacity duration-300 hover:opacity-80"
                  ></circle>
                }
              </svg>
              @if (chart.type === 'doughnut') {
                <div
                  class="absolute inset-0 flex flex-col items-center justify-center"
                >
                  <span class="text-2xl font-bold text-gray-900">{{
                    getTotalValue()
                  }}</span>
                  <span class="text-xs tracking-wide text-gray-500 uppercase"
                    >Total</span
                  >
                </div>
              }
            </div>
            <div class="flex flex-wrap justify-center gap-3">
              @for (item of chart.data; track item.label; let i = $index) {
                <div class="flex items-center gap-2">
                  <div
                    class="h-3 w-3 rounded-sm"
                    [style.background-color]="item.color || getDefaultColor(i)"
                  ></div>
                  <span class="text-xs text-gray-600"
                    >{{ item.label }}: {{ item.value }}</span
                  >
                </div>
              }
            </div>
          </div>
        }

        <!-- Line Chart -->
        @if (chart.type === 'line') {
          <div class="flex h-full flex-col">
            <div class="flex-1">
              <svg viewBox="0 0 400 200" class="h-48 w-full">
                <polyline
                  [attr.points]="getLinePoints()"
                  fill="none"
                  stroke="#3b82f6"
                  stroke-width="3"
                  class="drop-shadow-sm"
                ></polyline>
                @for (point of getPoints(); track $index) {
                  <circle
                    [attr.cx]="point.x"
                    [attr.cy]="point.y"
                    r="4"
                    fill="#3b82f6"
                    class="drop-shadow-sm"
                  ></circle>
                }
              </svg>
            </div>
            <div class="mt-3 flex justify-between px-5">
              @for (item of chart.data; track item.label) {
                <span class="text-xs text-gray-600">{{ item.label }}</span>
              }
            </div>
          </div>
        }
      </div>
    </div>
  `,
})
export class DashboardChartComponent implements OnInit {
  @Input() chart!: DashboardChart;

  private defaultColors = [
    '#3b82f6',
    '#10b981',
    '#f59e0b',
    '#ef4444',
    '#06b6d4',
    '#8b5cf6',
    '#ec4899',
    '#f97316',
  ];

  private maxValue = signal(0);

  ngOnInit() {
    this.maxValue.set(Math.max(...this.chart.data.map((item) => item.value)));
  }

  getDefaultColor(index: number): string {
    return this.defaultColors[index % this.defaultColors.length];
  }

  getBarHeight(value: number): number {
    return (value / this.maxValue()) * 100;
  }

  getTotalValue(): number {
    return this.chart.data.reduce((sum, item) => sum + item.value, 0);
  }

  pieSegments() {
    const total = this.getTotalValue();
    const circumference =
      2 * Math.PI * (this.chart.type === 'doughnut' ? 60 : 80);
    let currentOffset = 0;

    return this.chart.data.map((item, index) => {
      const percentage = item.value / total;
      const dashLength = circumference * percentage;
      const dashArray = `${dashLength} ${circumference}`;
      const dashOffset = -currentOffset;

      currentOffset += dashLength;

      return {
        dashArray,
        dashOffset,
        color: item.color || this.getDefaultColor(index),
      };
    });
  }

  getLinePoints(): string {
    const width = 400;
    const height = 200;
    const padding = 40;
    const dataWidth = width - 2 * padding;
    const dataHeight = height - 2 * padding;

    return this.chart.data
      .map((item, index) => {
        const x = padding + (index / (this.chart.data.length - 1)) * dataWidth;
        const y =
          height - padding - (item.value / this.maxValue()) * dataHeight;
        return `${x},${y}`;
      })
      .join(' ');
  }

  getPoints() {
    const width = 400;
    const height = 200;
    const padding = 40;
    const dataWidth = width - 2 * padding;
    const dataHeight = height - 2 * padding;

    return this.chart.data.map((item, index) => {
      const x = padding + (index / (this.chart.data.length - 1)) * dataWidth;
      const y = height - padding - (item.value / this.maxValue()) * dataHeight;
      return { x, y };
    });
  }
}
