import {
  EnvironmentProviders,
  inject,
  isDevMode,
  LOCALE_ID,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { provideTransloco } from '@jsverse/transloco';
import { TranslocoHttpLoader } from './transloco-loader';
import { LanguageService } from './language.service';
import { DEFAULT_LANG, LOCALE_DATA_CODES, SUPPORTED_CODES } from './supported-languages';

/**
 * Configura la internacionalización de la UI estática (misma mecánica que SDS FrontOffice):
 *  - Transloco (traducciones runtime desde assets/i18n/<lang>.json).
 *  - Detecta y aplica el idioma del navegador/SO al arrancar.
 *  - LOCALE_ID = idioma inicial (para los pipes date/number/currency).
 *
 * El `Accept-Language` en cada request lo pone el interceptor (lee LanguageService).
 */
export function provideI18n(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideTransloco({
      config: {
        availableLangs: SUPPORTED_CODES,
        defaultLang: DEFAULT_LANG,
        fallbackLang: DEFAULT_LANG,
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
        missingHandler: { useFallbackTranslation: true, logMissingKey: isDevMode() },
      },
      loader: TranslocoHttpLoader,
    }),

    // Al arrancar: resolver el idioma (override guardado → navegador/SO → base) y aplicarlo.
    provideAppInitializer(() => {
      const lang = inject(LanguageService);
      lang.setLanguage(lang.resolveInitialLang(), false);
    }),

    // LOCALE_ID = idioma inicial si tiene datos de locale de Angular; si no, el base.
    {
      provide: LOCALE_ID,
      useFactory: () => {
        const initial = inject(LanguageService).resolveInitialLang();
        return LOCALE_DATA_CODES.includes(initial) ? initial : DEFAULT_LANG;
      },
    },
  ]);
}
