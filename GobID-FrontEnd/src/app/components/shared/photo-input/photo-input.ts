import {
  Component,
  EventEmitter,
  inject,
  Input,
  OnDestroy,
  Output,
  signal,
  ViewChild,
} from '@angular/core';
import {
  ControlValueAccessor,
  FormControl,
  NgControl,
} from '@angular/forms';
import { TitleCasePipe } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faCamera,
  faCrosshairs,
  faMagnifyingGlassMinus,
  faMagnifyingGlassPlus,
  faRotateLeft,
  faRotateRight,
  faUser,
} from '@fortawesome/free-solid-svg-icons';
import {
  ImageCroppedEvent,
  ImageCropperComponent,
  ImageTransform,
} from 'ngx-image-cropper';
import { HandleErrorsMessages } from '../utils';
import { ErrorLabelComponent } from '../error-label/error-label.component';
import { ModalComponent } from '../modal/modal';

@Component({
  selector: 'app-photo-input',
  templateUrl: './photo-input.html',
  imports: [
    TitleCasePipe,
    FontAwesomeModule,
    ErrorLabelComponent,
    ImageCropperComponent,
    ModalComponent,
  ],
})
export class PhotoInputComponent implements ControlValueAccessor, OnDestroy {
  @Input() label = '';
  @Input() name = 'photo';
  @Input() className = '';
  @Input() isSubmitted = false;
  @Input() loading = false;
  @Input() required = false;
  @Input() accept = 'image/*';
  @Input() maxSizeMB = 5;
  @Output() fileSelected = new EventEmitter<File | null>();

  @ViewChild('cropModal') cropModal!: ModalComponent;
  @ViewChild(ImageCropperComponent) imageCropperRef?: ImageCropperComponent;

  previewUrl = signal<string | null>(null);
  pendingFile = signal<File | null>(null);
  transform = signal<ImageTransform>({});

  faCamera = faCamera;
  faUser = faUser;
  faRotateLeft = faRotateLeft;
  faRotateRight = faRotateRight;
  faMagnifyingGlassPlus = faMagnifyingGlassPlus;
  faMagnifyingGlassMinus = faMagnifyingGlassMinus;
  faCrosshairs = faCrosshairs;

  private objectUrl: string | null = null;
  private croppedBlob: Blob | null = null;

  public controlDir = inject(NgControl, { self: true });

  constructor() {
    this.controlDir.valueAccessor = this;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';

    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.control.setValue(null);
      this.control.setErrors({ invalidFileType: true });
      this.control.markAsTouched();
      return;
    }

    if (file.size > this.maxSizeMB * 1024 * 1024) {
      this.control.setValue(null);
      this.control.setErrors({ fileTooLarge: { maxSizeMB: this.maxSizeMB } });
      this.control.markAsTouched();
      return;
    }

    this.croppedBlob = null;
    this.transform.set({});
    this.pendingFile.set(file);
    this.cropModal.toggleModal(true);
  }

  onImageCropped(event: ImageCroppedEvent): void {
    this.croppedBlob = event.blob ?? null;
  }

  rotateLeft(): void {
    this.transform.update((t) => ({ ...t, rotate: (t.rotate ?? 0) - 90 }));
  }

  rotateRight(): void {
    this.transform.update((t) => ({ ...t, rotate: (t.rotate ?? 0) + 90 }));
  }

  zoomIn(): void {
    this.transform.update((t) => ({ ...t, scale: (t.scale ?? 1) + 0.1 }));
  }

  zoomOut(): void {
    this.transform.update((t) => ({
      ...t,
      scale: Math.max(0.1, (t.scale ?? 1) - 0.1),
    }));
  }

  centerImage(): void {
    this.transform.set({});
    this.imageCropperRef?.resetCropperPosition();
  }

  confirmEdit(): void {
    const file = this.pendingFile();
    const blob = this.croppedBlob;
    if (!file || !blob) return;

    const editedFile = new File([blob], file.name, {
      type: blob.type || file.type,
    });

    this.setPreviewFromFile(editedFile);
    this.control.setValue(editedFile);
    this.control.markAsTouched();
    this.control.markAsDirty();
    this.fileSelected.emit(editedFile);
    this.closeEditor();
  }

  cancelEdit(): void {
    this.closeEditor();
  }

  onError = () => {
    return HandleErrorsMessages(this.control);
  };

  writeValue(value: File | string | null): void {
    if (value instanceof File) {
      this.setPreviewFromFile(value);
      return;
    }

    if (typeof value === 'string' && value) {
      this.revokeObjectUrl();
      this.previewUrl.set(value);
      return;
    }

    this.clearPreview();
  }

  registerOnChange(): void {
    return;
  }

  registerOnTouched(): void {
    return;
  }

  ngOnDestroy(): void {
    this.revokeObjectUrl();
  }

  get control(): FormControl {
    return this.controlDir.control as FormControl;
  }

  private closeEditor(): void {
    this.cropModal.toggleModal(false);
    this.pendingFile.set(null);
    this.croppedBlob = null;
    this.transform.set({});
  }

  private setPreviewFromFile(file: File): void {
    this.revokeObjectUrl();
    this.objectUrl = URL.createObjectURL(file);
    this.previewUrl.set(this.objectUrl);
  }

  private clearPreview(): void {
    this.revokeObjectUrl();
    this.previewUrl.set(null);
  }

  private revokeObjectUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
    }
  }
}
