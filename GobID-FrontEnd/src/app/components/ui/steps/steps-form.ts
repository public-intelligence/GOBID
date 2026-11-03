import { Component, Input, OnChanges, signal } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faCheck } from '@fortawesome/free-solid-svg-icons';
import { StepForm } from '../../../models';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-steps-form',
  templateUrl: './steps-form.html',
  imports: [FontAwesomeModule, NgClass],
})
export class StepsForm implements OnChanges {
  @Input() data!: StepForm;

  steps = signal<StepForm>(this.data ?? { current: 1, steps: [] });

  faCheck = faCheck;

  ngOnChanges() {
    this.steps.set(this.data ?? { current: 1, steps: [] });
  }

  nextStep() {
    if (this.steps().current >= this.steps().steps.length) {
      return;
    }
    this.steps.set({ ...this.steps(), current: this.steps().current + 1 });
  }

  previousStep() {
    if (this.steps().current <= 1) {
      return;
    }
    this.steps.set({ ...this.steps(), current: this.steps().current - 1 });
  }

  goToStep(stepIndex: number) {
    this.steps.set({ ...this.steps(), current: stepIndex });
  }
}
