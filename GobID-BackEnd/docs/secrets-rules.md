# Reglas de manejo de secretos en GOB-ID

Vigentes desde el 31 de julio de 2026, cuando los secretos de GOB-ID pasaron a Azure Key
Vault. Este archivo va trackeado a propósito: una regla que vive en la memoria de alguien
se rompe en el primer descuido.

## Dónde vive cada cosa

| Ambiente | Dónde están los secretos | Quién los lee |
|---|---|---|
| Local | `user-secrets` de .NET (ver `local-setup.md`) | El desarrollador |
| QA | `KV-GOBID-VUC-DEV`, expuestos como referencias en App Settings | La identidad `ID-GOBID-VUC-DEV` |
| Producción | `KV-GOBID-VUC-PROD` | La identidad `ID-GOBID-VUC-PROD` |

En el repositorio, un secreto **siempre** es cadena vacía.

## Las cuatro reglas

### 1. Nunca un valor de secreto en `appsettings*.json`

Ni real, ni de ejemplo, ni "temporal para probar". Cadena vacía siempre. El historial de
Git no se puede limpiar: un secreto commiteado queda expuesto para siempre y obliga a
rotarlo — con todo lo que eso arrastra.

Para documentar que una clave existe, agrégala a `GOBID.Api/secrets.example.json`
**sin valor**.

### 2. No renombrar los App Settings de secretos

Estos siete settings ya **no contienen valores**: contienen referencias al vault que
apuntan al nombre exacto del setting.

```
connectionStrings:default          Jwt:Key           Jwt:AppKey
Exceptionless:ApiKey               GraphMailSettings:ClientSecret
PdsApiSettings:M2MClientSecret     DmsApiSettings:clientSecret
```

Si se renombra uno, la referencia queda huérfana y **la aplicación recibe el texto literal
`@Microsoft.KeyVault(...)` en vez del secreto**. No falla al desplegar: falla en tiempo de
ejecución, con un error que no menciona el rename. Si hace falta renombrar, se coordina
para actualizar la referencia en el mismo cambio.

Nota: `ConnectionStrings:DefaultConnection` fue eliminado. Estaba duplicado y ningún código
lo leía; la cadena que el código usa es `connectionStrings:default`.

### 3. No inyectar secretos desde el pipeline

En `azure-pipelines-gobid-api.yml` se eliminó la línea que inyectaba la cadena de conexión
como App Setting en cada despliegue (PR #1063). Hay un comentario en el YAML que explica
por qué no debe volver.

Si vuelve, **el siguiente despliegue sobreescribe la referencia con un valor en texto plano
y deshace la migración en silencio**: el servicio sigue funcionando, simplemente vuelve a
tener la credencial expuesta.

### 4. Un secreto nuevo se agrega al vault, no al repositorio

El flujo es: se carga el secreto al vault → se crea el App Setting como referencia → el
código lo lee igual que siempre (`_config["MiSeccion:MiClave"]`). Sin cambios de código.
Coordinarlo con quien administre el vault.

## Por qué existen estas reglas

Estos secretos estaban en texto plano en el repositorio, y cuatro de ellos eran idénticos
a los valores en uso en QA — incluida `Jwt:Key`, la clave maestra de la que se derivan las
llaves de firma de cada aplicación. Cualquiera con acceso al historial del repositorio los
tenía. Vaciar los archivos cierra la puerta hacia adelante; las reglas de arriba son lo que
evita que se vuelva a abrir.
