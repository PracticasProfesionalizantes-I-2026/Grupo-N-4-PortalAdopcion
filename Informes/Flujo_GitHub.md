# Ramas y revisión cruzada

El reparto de Plan_Ramas.csv es sugerido. Todavía no fue acordado, publicado ni revisado en GitHub. La copia concentra cambios para leerlos; se deben trasladar por incrementos al repositorio original.

1. Actualizar main y crear la rama del caso elegido.
2. Adaptar al repositorio los cambios de ese CU y sus dependencias acordadas; no copiar toda la implementación en cada rama.
3. Incluir contrato, negocio, datos, migración, pruebas y documentación.
4. Ejecutar build y test; documentar resultados.
5. Publicar rama y abrir PR hacia main, solicitando revisión del otro integrante.
6. Corregir comentarios en la misma rama. El compañero debe revisar el último commit y aprobar antes de integrar.

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/cu-09-catalogo-mascotas
# implementar el incremento
dotnet build PortalAdopcion.slnx
dotnet test PortalAdopcion.slnx
git status
git add <archivos-revisados>
git commit -m "CU-09: completar catalogo de mascotas"
git push -u origin feature/cu-09-catalogo-mascotas
```

Los borradores de propuestas-pr indican qué código estudiar. Solo describir como completado lo que realmente tenga el PR. No aprobar un PR propio, no abrir PR vacíos y no afirmar que la demo ya tuvo revisión cruzada.

Orden por dependencias: identidad → catálogo/blacklist/postulación → evaluación/entrevista → clínica/seguimiento → cancelación/abandono/finalización → reportes/control. La auditoría se crea como soporte temprano; su consulta se entrega en CU-17.

CU-05 sigue siendo una subfunción: un PR separado puede completar su confirmación/auditoría después de CU-02 sin crear otra entidad; si no hay cambio independiente, acordar un PR CU-02/CU-05 con el docente.
