import { Component, inject, OnInit, signal } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { sideData } from './config';
import { NgClass, TitleCasePipe } from '@angular/common';
import { SideData } from '../../../models';
import {
  faAnglesLeft,
  faAnglesRight,
  faAngleRight,
  faArrowRightFromBracket,
} from '@fortawesome/free-solid-svg-icons';
import { NavigationEnd, Router } from '@angular/router';
import { AuthService } from '../../../services';
import { filter } from 'rxjs';
import { CheckPermissions } from '../../../directives/check-permission';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [FontAwesomeModule, TitleCasePipe, NgClass, CheckPermissions],
  templateUrl: './sidebar.html',
})
export class SidebarComponent implements OnInit {
  faArrowRightFromBracket = faArrowRightFromBracket;
  faAnglesLeft = faAnglesLeft;
  faAnglesRight = faAnglesRight;
  faAngleRight = faAngleRight;

  protected currentPath = signal(window.location.pathname || '/');
  protected sideData = signal<SideData[]>(sideData);
  protected isCollapse = signal(false);

  private router = inject(Router);
  private authService = inject(AuthService);

  ngOnInit(): void {
    this.calculatePixelsOnScreen();
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        this.onNavigate(event.urlAfterRedirects);
      });

    window.addEventListener('resize', this.calculatePixelsOnScreen);
    this.findAndUncollapse(this.currentPath());
  }

  public handleToggleSidebar(): void {
    this.isCollapse.set(!this.isCollapse());
    if (!this.isCollapse()) return;
    this.closeSubElements();
  }

  closeSubElements() {
    this.sideData.set(
      this.sideData().map((data) => {
        if (data.withElements) {
          return { ...data, collapse: true };
        }
        return data;
      }),
    );
  }

  findAndUncollapse(path: string) {
    this.sideData().forEach((data) => {
      if (data.path === path) {
        data.collapse = false;
      }
      if (data.elements && data.elements.length > 0) {
        data.elements.forEach((element) => {
          if (element.path === path) {
            data.collapse = false;
          }
        });
      }
    });
  }

  /**
   * Actualiza el currentPath y colapsa/expande según la ruta.
   */
  private updateCurrentPath(path: string) {
    this.sideData().forEach((data) => {
      if (data.path && path.includes(data.path)) {
        this.currentPath.set(data.path);
        return;
      }
      if (data.elements && data.elements.length > 0) {
        data.elements.forEach((element) => {
          if (element.path && path.includes(element.path)) {
            this.currentPath.set(element.path);
          }
        });
      }
    });
  }

  /**
   * Cambia el estado del sidebar en caso de que el ancho de la pantalla sea menor a 1024px.
   */
  calculatePixelsOnScreen = () => {
    const width = window.innerWidth;
    if (width > 1024) return;
    this.isCollapse.set(true);
    this.closeSubElements();
  };

  protected onNavigate(path?: string, hasElement?: boolean, id?: number) {
    if (hasElement) {
      this.sideData.set(
        this.sideData().map((data) => {
          if (data.id !== id) {
            return data;
          }
          return { ...data, collapse: !data.collapse };
        }),
      );
    }
    if (this.isCollapse() && hasElement) {
      this.isCollapse.set(false);
    }
    if (!path) return;
    this.router.navigate([path]);
    this.updateCurrentPath(path);
    if (window.innerWidth > 768) return;
    this.isCollapse.set(true);
  }

  protected onLogout(): void {
    this.authService.onLogout();
  }
}
