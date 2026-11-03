import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import {
  AdminLayoutComponent,
  LayoutService,
  SidebarMenuItem,
  UserProfile,
} from '@public-intelligence/ui-core';
import { sideData, MenuItemWithPermissions } from './config';
import { PersonService, TokenDecoder } from '../../../../services';
import { AuthService } from '../../../../services/auth/auth.service';
import { Subscription } from 'rxjs';

const DEFAULT_AVATAR_URL = 'assets/images/user-avatar.svg';

@Component({
  selector: 'app-main.layout',
  imports: [AdminLayoutComponent],
  templateUrl: './main.layout.html',
})
export class MainLayout implements OnInit, OnDestroy {
  private tokenDecoderService = inject(TokenDecoder);
  private authService = inject(AuthService);
  private layoutService = inject(LayoutService);
  private personService = inject(PersonService);

  private actionSub?: Subscription;
  private photoSub?: Subscription;
  private avatarObjectUrl?: string;

  protected menuItems = signal<SidebarMenuItem[]>([]);
  protected branding = {
    appName: 'GOBID',
    moduleName: 'Modulo',
    logoUrl: 'assets/images/cupula-gob-azul.webp',
  };
  protected userProfile = signal<UserProfile | null>(null);

  ngOnInit() {
    this.menuItems.set(this.getFilteredMenuItems());
    this.setUserProfile();

    this.layoutService.profileMenuItems.set([
      { label: 'Cerrar sesión', action: 'logout', destructive: true },
    ]);

    this.actionSub = this.layoutService.profileMenuAction$.subscribe(
      (action) => {
        if (action === 'logout') this.authService.onLogout();
      },
    );
  }

  ngOnDestroy() {
    this.actionSub?.unsubscribe();
    this.photoSub?.unsubscribe();
    if (this.avatarObjectUrl) URL.revokeObjectURL(this.avatarObjectUrl);
  }

  private hasAnyPermission(
    itemPermissions: string[],
    userPermissions: string[],
  ): boolean {
    return itemPermissions.some((p) => userPermissions.includes(p));
  }

  private filterItems(
    items: MenuItemWithPermissions[],
    userPermissions: string[],
  ): SidebarMenuItem[] {
    return items
      .filter(
        (item) =>
          !item.permissions?.length ||
          this.hasAnyPermission(item.permissions, userPermissions),
      )
      .map(({ permissions, children, ...rest }) => ({
        ...rest,
        ...(children?.length
          ? { children: this.filterItems(children, userPermissions) }
          : {}),
      }));
  }

  private getFilteredMenuItems(): SidebarMenuItem[] {
    const permissions = this.tokenDecoderService.getPermissions();
    return this.filterItems(sideData, permissions);
  }

  private setUserProfile() {
    this.userProfile.set({
      name: this.tokenDecoderService.getPersonFullName(),
      role: this.tokenDecoderService.getOrganizationName(),
      avatarUrl: DEFAULT_AVATAR_URL,
    });

    const personId = this.tokenDecoderService.getPersonId();
    this.photoSub = this.personService.getPhotobyPersonId(personId).subscribe({
      next: (blob) => {
        this.avatarObjectUrl = URL.createObjectURL(blob);
        const withPhoto = (profile: UserProfile | null) =>
          profile ? { ...profile, avatarUrl: this.avatarObjectUrl! } : profile;
        this.layoutService.userProfile.update(withPhoto);
      },
      error: () => {
        // La persona puede no tener foto asignada (404); se conserva el avatar por defecto.
      },
    });
  }
}
