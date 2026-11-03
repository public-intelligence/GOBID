import { Component, signal, ViewChild } from '@angular/core';
import { ModalComponent } from '../../../shared';

@Component({
  selector: 'app-info-modal',
  imports: [ModalComponent],
  templateUrl: './info-modal.html',
})
export class InfoModalComponent {
  title = signal('');
  paragraph = signal('');
  name = signal('');
  confirmButton = signal('');
  onConfirm!: () => void;

  @ViewChild(ModalComponent) modalComponent!: ModalComponent;

  /**
   * Cierra el modal de confirmación
   */
  closeModal() {
    this.modalComponent.toggleModal(false);
  }

  /**
   * Abre el modal de confirmación
   * @param title Título del modal
   * @param paragraph Texto del modal
   * @param name Nombre del elemento a eliminar
   * @param confirmButton Texto del botón de confirmación
   * @param onConfirm Función a ejecutar al confirmar
   */
  openModal({
    title,
    paragraph,
    name,
    confirmButton,
    onConfirm,
  }: {
    title: string;
    name: string;
    paragraph: string;
    confirmButton: string;
    warning?: string;
    onConfirm: () => void;
  }) {
    this.title.set(title);
    this.name.set(name);
    this.paragraph.set(paragraph);
    this.confirmButton.set(confirmButton);
    this.onConfirm = () => {
      onConfirm();
      this.closeModal();
    };
    this.modalComponent.toggleModal(true);
  }
}
