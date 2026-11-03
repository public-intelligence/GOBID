export interface Step {
  id: number;
  title: string;
}

export interface StepForm {
  current: number;
  steps: Step[];
}
