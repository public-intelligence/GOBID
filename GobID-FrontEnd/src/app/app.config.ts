import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';
import localeEn from '@angular/common/locales/en';
import localeFr from '@angular/common/locales/fr';
import localeDe from '@angular/common/locales/de';
import localeIt from '@angular/common/locales/it';
import localePt from '@angular/common/locales/pt';
import localeZh from '@angular/common/locales/zh';
import localeAr from '@angular/common/locales/ar';
import localeRu from '@angular/common/locales/ru';
import localeJa from '@angular/common/locales/ja';
import localeKo from '@angular/common/locales/ko';
import localeNl from '@angular/common/locales/nl';

import { routes } from './app.routes';
import {
  provideHttpClient,
  withFetch,
  withInterceptors,
} from '@angular/common/http';
import { authInterceptor } from './interceptors/auth.interceptor';
import { provideI18n } from './core/i18n/i18n.providers';

// Datos de locale para los pipes date/number/currency. Uno por cada idioma con
// `hasLocaleData: true` en supported-languages.ts.
registerLocaleData(localeEs, 'es');
registerLocaleData(localeEn, 'en');
registerLocaleData(localeFr, 'fr');
registerLocaleData(localeDe, 'de');
registerLocaleData(localeIt, 'it');
registerLocaleData(localePt, 'pt');
registerLocaleData(localeZh, 'zh');
// 'ht' (Kreyòl) no tiene datos de locale en Angular 20 — se omite (hasLocaleData:false).
registerLocaleData(localeAr, 'ar');
registerLocaleData(localeRu, 'ru');
registerLocaleData(localeJa, 'ja');
registerLocaleData(localeKo, 'ko');
registerLocaleData(localeNl, 'nl');

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    provideI18n(),
  ],
};
