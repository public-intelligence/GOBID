import { Component, inject, Input, signal, ViewChild } from '@angular/core';
import { ModalComponent } from '../../../shared';
import { NgClass, TitleCasePipe } from '@angular/common';
import { DisplayData } from '../../../../models';
import { UtilsService } from '../../../../services';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faPen } from '@fortawesome/free-solid-svg-icons';
import { Router } from '@angular/router';

@Component({
  selector: 'app-view-info',
  imports: [ModalComponent, TitleCasePipe, NgClass, FontAwesomeModule],
  templateUrl: './view-info.html',
})
export class ViewInfo {
  @ViewChild(ModalComponent) modalComponent!: ModalComponent;
  @Input() title = '';
  @Input() canEdit = true;

  faPen = faPen;
  editPath = '';

  protected data = signal<DisplayData[]>([]);

  protected utilsService = inject(UtilsService);
  protected router = inject(Router);

  openModal = (data: DisplayData[], editPath = ''): void => {
    if (!(data && data.length > 0)) return;
    this.modalComponent.toggleModal(true);
    this.data.set(data);
    this.editPath = editPath;
  };

  goToEdit(): void {
    if (!this.editPath) return;
    this.router.navigate([this.editPath]);
  }
}
