import { Component, Input, signal } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-modal',
  imports: [FontAwesomeModule],
  templateUrl: './modal.html',
})
export class ModalComponent {
  @Input() title = '';
  @Input() hideOnClickOutside = false;
  @Input() isConfirmation = false;
  @Input() faXmark = faXmark;
  @Input() showCloseIcon = true;
  @Input() onClose?: () => void;

  public show = signal(false);

  toggleModal(bool: boolean) {
    this.show.set(bool);
  }

  closeModal() {
    this.show.set(false);
    if (!this.onClose) return;
    this.onClose();
  }
}
