using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using CelexWebApp.Models.AlumnoMMV;

namespace CelexWebApp.Models.AdministradorMMV
{
    public class HistorialCelexDocument : IDocument
    {
        public decimal calificaciontotaltotal { get; set; } 
        public decimal asistenciaTotal { get; set; }
        public DateTime Fecha { get; set; }
        public List<HistorialAvanceAlumno> Historial { get; set; }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));
                page.Header().Text("CECyT No.13\n“Ricardo Flores Magón”").Bold().FontSize(16).AlignCenter();

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text($"CELEX/CECYT13/H0109-2024").Bold();

                    column.Item().Text("A QUIEN CORRESPONDA:");

                    column.Item().Text(text =>
                    {
                        text.Span("Con base en la documentación que obra en los expedientes de los Cursos Extracurriculares de Lenguas Extranjeras (CELEX), se hace constar que ");
                        text.Span(Historial[0].Nombre).Bold();
                        text.Span($", con número de boleta ");
                        text.Span(Historial[0].Boleta).Bold();
                        text.Span(" ha cursado los siguientes módulos del idioma inglés como se detalla:");
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("MÓDULO").Bold();
                            header.Cell().Text("NIVEL MCER").Bold();
                            header.Cell().Text("CURSO").Bold();
                            header.Cell().Text("PERIODO").Bold();
                            header.Cell().Text("HORAS").Bold();
                            header.Cell().Text("CALIFICACIÓN").Bold();
                        });

                        foreach (var item in Historial)
                        {
                            table.Cell().Text($"{item.Modulo}");
                            table.Cell().Text($"{item.NivelMCER}");
                            table.Cell().Text($"{item.NombreCurso}");
                            table.Cell().Text($"{item.Periodo}");
                            table.Cell().Text($"{item.Asistencia}"); 
                            table.Cell().Text($"{item.CalificacionTotal}");
                        }
                    });
                    column.Item().Text($"\nTotal de horas de asistencia: {asistenciaTotal}").Bold();
                    column.Item().Text($"Promedio general de calificación: {calificaciontotaltotal:F1}").Bold();

                    column.Item().Text("\nEl historial muestra que el usuario ha cursado los estudios correspondientes al nivel B2 de acuerdo con el Marco Común Europeo de Referencia para las Lenguas (MCER).");

                    column.Item().Text($"\nA petición de la interesada y para los fines académicos que considere convenientes, se extiende la presente en la Ciudad de México, a los {Fecha:dd} días del mes de {Fecha:MMMM} del año {Fecha:yyyy}.");

                    column.Item().Text("\nATENTAMENTE\n“LA TÉCNICA AL SERVICIO DE LA PATRIA”").AlignCenter();

                    column.Item().Text("\nMTRO. MIGUEL ANGEL CRUZ RODRÍGUEZ\nSUBDIRECCIÓN ACADÉMICA").AlignLeft();
                    column.Item().Text("ING. FERNANDO MARTÍNEZ SÁNCHEZ\nSUBDIRECCIÓN DE SERVICIOS EDUCATIVOS E INTEGRACIÓN SOCIAL").AlignLeft();
                    column.Item().Text("MTRA. PATRICIA BALTAZAR TRUJILLO\nDIRECTORA").AlignLeft();
                });

                page.Footer().AlignCenter().Text("Programa registrado ante la DFLE: DFLE-CELEXCECYT13IACT06-17").FontSize(10);
            });
        }
    }
}
