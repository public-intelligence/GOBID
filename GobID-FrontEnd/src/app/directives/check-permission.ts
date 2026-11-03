import {
  Directive,
  Input,
  TemplateRef,
  ViewContainerRef,
  inject,
} from '@angular/core';
import { UtilsService } from '../services';

@Directive({
  selector: '[appCheckPermissions]',
})
export class CheckPermissions {
  private templateRef = inject(TemplateRef);
  private viewContainer = inject(ViewContainerRef);
  private utilsService = inject(UtilsService);

  @Input() set appCheckPermissions(permissions: string[]) {
    const hasPermission = this.utilsService.hasPermission(permissions);
    this.viewContainer.clear();
    if (hasPermission) {
      this.viewContainer.createEmbeddedView(this.templateRef);
    }
  }
}
