/**
 * Idiomas soportados por GOB-ID (login + registro, cara al ciudadano).
 *
 * El idioma por defecto se detecta del SISTEMA OPERATIVO / navegador (navigator.languages);
 * puede sobreescribirse manualmente (selector) y se persiste. Cada idioma habilitado aquí DEBE:
 *   - tener su archivo de traducción en `src/assets/i18n/<code>.json`
 *   - (si `hasLocaleData`) tener su locale registrado en `app.config.ts` para los pipes date/number.
 *
 * Para AGREGAR un idioma: añade una entrada aquí, crea su `<code>.json` y registra su locale en
 * `app.config.ts`. La infra soporta cualquier cantidad de idiomas (misma mecánica que SDS FrontOffice).
 */
export interface AppLanguage {
  /** Código BCP-47 base (ej. "es", "en", "fr"). */
  code: string;
  /** Nombre en su propio idioma, para el selector. */
  native: string;
  /** Dirección del texto (ltr / rtl para árabe, hebreo, etc.). */
  dir: 'ltr' | 'rtl';
  /** ¿Angular tiene datos de locale para date/number? (si no, cae al default para formato). */
  hasLocaleData: boolean;
}

export const SUPPORTED_LANGUAGES: AppLanguage[] = [
  { code: 'es', native: 'Español', dir: 'ltr', hasLocaleData: true },
  { code: 'en', native: 'English', dir: 'ltr', hasLocaleData: true },
  { code: 'fr', native: 'Français', dir: 'ltr', hasLocaleData: true },
  { code: 'de', native: 'Deutsch', dir: 'ltr', hasLocaleData: true },
  { code: 'it', native: 'Italiano', dir: 'ltr', hasLocaleData: true },
  { code: 'pt', native: 'Português', dir: 'ltr', hasLocaleData: true },
  { code: 'zh', native: '中文', dir: 'ltr', hasLocaleData: true },
  // Angular 20's @angular/common/locales no trae 'ht' → sin datos de locale
  // (Transloco igual traduce el texto; los pipes date/number caen a 'es').
  { code: 'ht', native: 'Kreyòl ayisyen', dir: 'ltr', hasLocaleData: false },
  { code: 'ar', native: 'العربية', dir: 'rtl', hasLocaleData: true },
  { code: 'ru', native: 'Русский', dir: 'ltr', hasLocaleData: true },
  { code: 'ja', native: '日本語', dir: 'ltr', hasLocaleData: true },
  { code: 'ko', native: '한국어', dir: 'ltr', hasLocaleData: true },
  { code: 'nl', native: 'Nederlands', dir: 'ltr', hasLocaleData: true },
];

/** Idioma base / fallback. */
export const DEFAULT_LANG = 'es';

/** Lista de códigos habilitados (para Transloco availableLangs). */
export const SUPPORTED_CODES: string[] = SUPPORTED_LANGUAGES.map((l) => l.code);

/** Códigos con datos de locale de Angular registrados (para LOCALE_ID / pipes). */
export const LOCALE_DATA_CODES: string[] = SUPPORTED_LANGUAGES.filter((l) => l.hasLocaleData).map((l) => l.code);
