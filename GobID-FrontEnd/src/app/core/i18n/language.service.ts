import { inject, Injectable, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { DEFAULT_LANG, SUPPORTED_CODES, SUPPORTED_LANGUAGES } from './supported-languages';

const STORAGE_KEY = 'gobid.lang';

/**
 * Resuelve y aplica el idioma de la app (misma mecánica que SDS FrontOffice).
 *
 * Prioridad del idioma inicial: override manual guardado → idioma del SISTEMA OPERATIVO
 * (navigator.languages) → idioma base. Al cambiar de idioma, actualiza Transloco, el
 * `<html lang>`/`dir`, y lo persiste. El interceptor HTTP lee `activeLang` para mandar
 * `Accept-Language` en cada petición al backend.
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);

  /** Idioma activo como signal (para bindings/selector en templates). */
  readonly active = signal<string>(DEFAULT_LANG);

  /** Idioma inicial: override guardado → SO → base. (No aplica nada; solo resuelve.) */
  resolveInitialLang(): string {
    const saved = this.readSaved();
    if (saved && SUPPORTED_CODES.includes(saved)) return saved;
    return this.detectFromOs() ?? DEFAULT_LANG;
  }

  /** Primer idioma del SO que esté soportado (navigator.languages → navigator.language). */
  private detectFromOs(): string | null {
    const prefs = navigator.languages?.length ? navigator.languages : [navigator.language];
    for (const pref of prefs ?? []) {
      if (!pref) continue;
      const base = pref.toLowerCase().split('-')[0];
      if (SUPPORTED_CODES.includes(base)) return base;
    }
    return null;
  }

  /** Aplica un idioma: Transloco + <html lang/dir> + persistencia (opcional). */
  setLanguage(code: string, persist = true): void {
    const lang = SUPPORTED_CODES.includes(code) ? code : DEFAULT_LANG;
    this.transloco.setActiveLang(lang);
    this.active.set(lang);

    const meta = SUPPORTED_LANGUAGES.find((l) => l.code === lang);
    const html = document.documentElement;
    html.lang = lang;
    html.dir = meta?.dir ?? 'ltr';

    if (persist) this.saveSaved(lang);
  }

  /** Idioma activo (lo usa el interceptor para `Accept-Language`). */
  getActiveLang(): string {
    return this.active();
  }

  /** Lista de idiomas disponibles (para un selector). */
  get languages() {
    return SUPPORTED_LANGUAGES;
  }

  private readSaved(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null; // modo privado / storage bloqueado
    }
  }

  private saveSaved(value: string): void {
    try {
      localStorage.setItem(STORAGE_KEY, value);
    } catch {
      /* no-op: sin persistencia si el storage está bloqueado */
    }
  }
}
