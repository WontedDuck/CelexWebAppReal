using CelexWebApp.Models.ProfesorMMV;
using Microsoft.AspNetCore.Mvc;

namespace CelexWebApp.Controllers.Profesor
{
    public class LibrosController : Controller
    {
        private List<Libro> libros = new()
        {
            new Libro { Id = 1, Titulo = "Evolve 1 Student's Book", ArchivoPdf = "Evolve 1 Student's Book.pdf" },
            new Libro { Id = 18, Titulo = "Evolve 1 Teacher's Edition", ArchivoPdf = "Evolve 1 Teacher's Edition.pdf" },
            new Libro { Id = 2, Titulo = "Evolve 1 Workbook", ArchivoPdf = "Evolve 1 Workbook.pdf" },
            new Libro { Id = 3, Titulo = "Evolve 1. Photocopiable Activities Worksheets", ArchivoPdf = "Evolve 1. Photocopiable Activities Worksheets.pdf" },
            new Libro { Id = 4, Titulo = "Evolve 2 Student's Book", ArchivoPdf = "Evolve 2 Student's Book.pdf" },
            new Libro { Id = 5, Titulo = "Evolve 2 Teacher's Edition", ArchivoPdf = "Evolve 2 Teacher's Edition.pdf" },
            new Libro { Id = 6, Titulo = "Evolve 2 Workbook", ArchivoPdf = "Evolve 2 Workbook.pdf" },
            new Libro { Id = 7, Titulo = "Evolve 2. Photocopiable Activities Worksheets", ArchivoPdf = "Evolve 2. Photocopiable Activities Worksheets.pdf" },
            new Libro { Id = 8, Titulo = "Evolve 3 Workbook", ArchivoPdf = "Evolve 3 Workbook.pdf" },
            new Libro { Id = 9, Titulo = "Evolve 4 Workbook", ArchivoPdf = "Evolve 4 Workbook.pdf" },
            new Libro { Id = 10, Titulo = "Evolve 4. Photocopiable Activities Worksheets", ArchivoPdf = "Evolve 4. Photocopiable Activities Worksheets.pdf" },
            new Libro { Id = 11, Titulo = "Evolve 4. Student's Book", ArchivoPdf = "Evolve 4. Student's Book.pdf" },
            new Libro { Id = 12, Titulo = "toaz.info-evolve-3-photocopiable-activities-worksheets-pr_0c99626532bf9f0720ccbe627340538a", ArchivoPdf = "toaz.info-evolve-3-photocopiable-activities-worksheets-pr_0c99626532bf9f0720ccbe627340538a.pdf" },
            new Libro { Id = 13, Titulo = "toaz.info-evolve-5-students-book-pr_abd23f72fc70db2e6383153067d20acf", ArchivoPdf = "toaz.info-evolve-5-students-book-pr_abd23f72fc70db2e6383153067d20acf.pdf" }
        };
        public IActionResult Index()
        {
            return View(libros);
        }
        public IActionResult Leer(int id)
        {
            var libro = libros.FirstOrDefault(l => l.Id == id);
            if (libro == null) return NotFound();

            ViewBag.PdfPath = $"/pdfs/{libro.ArchivoPdf}";
            ViewBag.Titulo = libro.Titulo;

            return View();
        }
    }
}
