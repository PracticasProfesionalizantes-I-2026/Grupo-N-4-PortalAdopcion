# Diagrama de flujo de estados — Portal de Adopción

> P3 del `Pendientes-Alta-Prioridad.md`. Fuente: transiciones válidas del código real
> (`BusinessLogic/Services/PortalService.cs`) y `Modelo-de-Estados-y-Reglas-de-Negocio.md`.
> Generado el 08/10/2026. Existe una versión PlantUML en `Diagrama-de-Estados.puml`.

Tres máquinas de estado relacionadas: **proceso (solicitud)**, **mascota** y **cuenta**.
Un proceso siempre arrastra el estado de su mascota (transiciones coloreadas).

---

## A. Proceso de adopción (`EstadoSolicitudEnum`)

Estados: `Pendiente · EnObservacion · Aprobada · EnEntrevista · EnPeriodoDePrueba · Adoptada`
(activos) y terminales `Rechazada · Cancelada · Abandonada`.

```mermaid
stateDiagram-v2
    [*] --> Pendiente : CU-02 Postular
    Pendiente --> EnObservacion : CU-04 Evaluar (observación)
    Pendiente --> Aprobada : CU-04 Evaluar (≥15 días)
    Pendiente --> Rechazada : CU-04 (rechazo) / CU-01 sanción
    Pendiente --> Cancelada : CU-04 mascota no disponible
    EnObservacion --> EnObservacion : CU-04 (re-evaluación)
    EnObservacion --> Aprobada : CU-04
    EnObservacion --> Rechazada : CU-04 / CU-01 sanción
    Pendiente --> Cancelada : CU-13 Voluntario/adoptante
    Aprobada --> EnEntrevista : CU-11 Programar entrevista
    EnEntrevista --> EnPeriodoDePrueba : CU-11 resultado favorable
    EnEntrevista --> Rechazada : CU-11 desfavorable
    EnEntrevista --> Abandonada : CU-14 timeout (7 días sin actividad)
    EnEntrevista --> Cancelada : CU-13
    EnPeriodoDePrueba --> Adoptada : CU-15 (≥días, historial, validación vet si alerta)
    EnPeriodoDePrueba --> Cancelada : CU-13 (+ sanción)
    Pendiente --> [*]
    EnPeriodoDePrueba --> [*]
    Rechazada --> [*]
    Cancelada --> [*]
    Abandonada --> [*]
    Aprobada --> [*]
    EnEntrevista --> [*]
    EnObservacion --> [*]
    Adoptada --> [*] : CU-15 (terminal OK)
```

Reglas: `Activa(e)` = Pendiente | EnObservacion | Aprobada | EnEntrevista | EnPeriodoDePrueba.
No hay estados "devueltos" a activos desde terminales (irreversibles).

---

## B. Mascota (`EstadoMascotaEnum`)

Estados: `Disponible · Reservada · EnEntrevista · EnPeriodoDePrueba · Adoptada · EnTratamiento · Inactiva`.

```mermaid
stateDiagram-v2
    [*] --> Disponible : CU-09 Alta (default)
    Disponible --> Reservada : CU-02 postulación
    Reservada --> Disponible : CU-04 rechazo / CU-01 sanción / CU-13 cancel
    Reservada --> EnEntrevista : CU-11 programar
    EnEntrevista --> EnPeriodoDePrueba : CU-11 favorable
    EnEntrevista --> Disponible : CU-11 desfavorable / CU-14 timeout / CU-13 cancel
    EnPeriodoDePrueba --> Adoptada : CU-15 finalizar
    EnPeriodoDePrueba --> Disponible : CU-13 cancel
    Disponible <--> EnTratamiento : CU-09 admin (ChangePet)
    Disponible <--> Inactiva : CU-09 admin (ChangePet)
    Adoptada --> [*] : CU-09 no cambia adoptadas
```

---

## C. Cuenta de usuario (`Usuario.Estado`, string)

Estados: `Activa · Suspendida · Baja` (soft-delete). Roles: Adoptante/Voluntario/Veterinario/Administrador.

```mermaid
stateDiagram-v2
    [*] --> Activa : CU-06 Registro / CU-08 alta personal
    Activa --> Suspendida : CU-08 (no Adoptante)
    Activa --> Baja : CU-08 / CU-09 baja de perfil
    Suspendida --> Activa : CU-08
    Suspendida --> Baja : CU-08
    Activa --> [*]
    Suspendida --> [*]
    Baja --> [*]
    note right of Activa
      Login: "Baja" bloquea a todos;
      "Suspendida" bloquea a personal (solo Adoptante entra).
      Blacklist NO es estado de cuenta:
      SancionBlacklist vigente bloquea postular (RN-01).
    end note
```

---

## Tabla de transiciones (fuente: `PortalService.cs`)

| CU | Método | De (solicitud) | A | Efecto mascota |
| --- | --- | --- | --- | --- |
| CU-02 | PostulateAsync | — | Pendiente | Disponible → Reservada |
| CU-04 | EvaluateAsync | Pendiente/EnObservacion | Aprobada (≥15 días) / EnObservacion / Rechazada | Rechazada → Disponible |
| CU-04 | EvaluateAsync (auto) | Pendiente/EnObservacion | Rechazada (sanción) / Cancelada (no disponible) | → Disponible |
| CU-01 | SaveBlacklistAsync | Activa | Rechazada | → Disponible |
| CU-11 | ScheduleAsync | Aprobada | EnEntrevista | Reservada → EnEntrevista |
| CU-11 | InterviewResultAsync | EnEntrevista | EnPeriodoDePrueba (favorable) / Rechazada | → EnPeriodoDePrueba / Disponible |
| CU-14 | AbandonAsync | EnEntrevista (idle > 7 días) | Abandonada | → Disponible |
| CU-13 | CancelAsync | Pendiente..EnPeriodoDePrueba | Cancelada (+ sanción si en prueba) | → Disponible |
| CU-15 | FinalizeAsync | EnPeriodoDePrueba | Adoptada | EnPeriodoDePrueba → Adoptada |
| CU-09 | ChangePetAsync | n/a | Disponible/EnTratamiento/Inactiva (admin) | Adoptada bloqueada |
| CU-08 | ChangeUserAsync | Activa/Suspendida | Suspendida/Baja | — |

Invariantes de acompañamiento: unicidad historial por mascota (CU-03/10), una única solicitud activa por mascota/adoptante (RN-04), mínimo 1 seguimiento diario (CU-12), responsabilidad del último administrador (CU-08), historial requerido antes de finalizar (CU-15).