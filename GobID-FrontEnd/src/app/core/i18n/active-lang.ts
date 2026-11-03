import { Signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, switchMap } from 'rxjs/operators';
import { TranslocoService } from '@jsverse/transloco';
import { DEFAULT_LANG, LOCALE_DATA_CODES } from './supported-languages';

/** Señales reactivas del idioma activo para componentes que traducen imperativamente. */
export interface ActiveLang {
  /** Idioma activo (código Transloco), ya con su catálogo de traducciones cargado. */
  readonly activeLang: Signal<string>;
  /**
   * Locale seguro para los pipes `date`/`number`/`currency`: si el idioma activo no tiene
   * datos de locale de Angular registrados, cae a `DEFAULT_LANG` para no romper el pipe.
   */
  readonly dateLocale: Signal<string>;
}

/**
 * Señal del idioma activo para componentes que traducen imperativamente (en `computed()`,
 * `effect()` o métodos del template OnPush). Centraliza el patrón
 * `langChanges$ → switchMap(load) → map` en un solo lugar.
 *
 * Debe llamarse en contexto de inyección (campo o constructor). `toSignal` se auto-limpia
 * vía el `DestroyRef` del componente — sin fugas.
 */
export function injectActiveLang(): ActiveLang {
  const transloco = inject(TranslocoService);
  const activeLang = toSignal(
    transloco.langChanges$.pipe(switchMap((l) => transloco.load(l).pipe(map(() => l)))),
    { initialValue: transloco.getActiveLang(), equal: () => false },
  );
  const dateLocale = computed(() => {
    const l = activeLang();
    return LOCALE_DATA_CODES.includes(l) ? l : DEFAULT_LANG;
  });
  return { activeLang, dateLocale };
}
