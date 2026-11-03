# Configurar GOB-ID en tu máquina

Los secretos ya no viven en el repositorio (los `appsettings*.json` llevan cadena vacía).
En tu máquina se guardan con **user-secrets** (el gestor de secretos de .NET), que los deja
fuera del proyecto para que no haya forma de subirlos por error. En QA los resuelve App
Service desde Key Vault; eso no lo tocas.

La guía sirve igual en macOS, Windows y Linux.

## Pasos (una sola vez)

1. **Pide el archivo de secretos de desarrollo a Remy.** Te llega por el canal seguro de la
   institución (nunca por correo, Teams ni WhatsApp). Se llama `gobid-dev-secrets.json` y
   trae los 7 valores completos.

   > Nota: el archivo también está guardado en **Pipelines → Library → Secure Files** del
   > proyecto GOB-ID, pero ese almacén es de solo escritura para las personas: únicamente
   > los pipelines pueden leerlo. Sirve como copia de respaldo controlada, no como punto
   > de descarga.

2. Guárdalo como `GOBID.Api/secrets.local.json` — está ignorado por git, así que los
   valores quedan solo en tu máquina.

3. Cárgalos todos de una:

   ```
   # macOS / Linux
   cat GOBID.Api/secrets.local.json | dotnet user-secrets set --project GOBID.Api

   # Windows PowerShell
   Get-Content -Raw GOBID.Api/secrets.local.json | dotnet user-secrets set --project GOBID.Api
   ```

4. Corre la API como siempre.

`GOBID.Api/secrets.example.json` (en el repo) es la referencia de qué claves existen — sin
valores. Los valores reales **nunca** van por git, chat ni correo.

## Ojo con la credencial de base de datos

La cadena del archivo trae el **usuario nuevo** (`gobIdUser_v2`). Si en tu máquina tenías la
cadena vieja con `gobIdUser`, reemplázala: ese usuario se deshabilita el
**14 de agosto de 2026**.
