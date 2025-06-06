using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using CelexWebApp.Models.AdministradorMMV;

public class HistorialProfesorDocument : IDocument
{
    private readonly List<GrupoDetallesModel> _grupos;

    public HistorialProfesorDocument(List<GrupoDetallesModel> grupos)
    {
        _grupos = grupos;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(20);
            page.Size(PageSizes.A4);

            page.Header().Text("Historial de Profesor")
            .FontSize(20)
            .Bold()
            .AlignCenter();

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2); 
                    columns.RelativeColumn(2);
                });
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("ID").Bold();
                    header.Cell().Element(CellStyle).Text("Nombre").Bold();
                    header.Cell().Element(CellStyle).Text("Ocupados").Bold();
                    header.Cell().Element(CellStyle).Text("Inicio").Bold();
                    header.Cell().Element(CellStyle).Text("Fin").Bold();
                    header.Cell().Element(CellStyle).Text("Tipo").Bold();
                    header.Cell().Element(CellStyle).Text("Nivel").Bold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container
                            .Background(Colors.Grey.Lighten2)
                            .Padding(5);
                    }
                });


                foreach (var grupo in _grupos)
                {
                    table.Cell().Text(grupo.Id.ToString());
                    table.Cell().Text(grupo.Nombre).WrapAnywhere();
                    table.Cell().Text(grupo.Ocupados.ToString());
                    table.Cell().Text(grupo.FechaInicio.ToString("dd/MM/yyyy"));
                    table.Cell().Text(grupo.FechaFin.ToString("dd/MM/yyyy"));
                    table.Cell().Text(grupo.TipoCurso).WrapAnywhere();
                    table.Cell().Text(grupo.Nivel).WrapAnywhere();
                }
            });
        });
    }
}
