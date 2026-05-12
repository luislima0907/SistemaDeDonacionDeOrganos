using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Services;
using ClosedXML.Excel;
using Newtonsoft.Json;
using SistemaDonacion.DTOs;
using SistemaDonacion.Models;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BitacoraController : ControllerBase
    {
        private readonly IBitacoraService _bitacoraService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BitacoraController(IBitacoraService bitacoraService, IHttpContextAccessor httpContextAccessor)
        {
            _bitacoraService = bitacoraService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Obtiene el historial de cambios de un registro específico
        /// </summary>
        [HttpGet("historial/{tabla}/{registroId}")]
        public async Task<IActionResult> ObtenerHistorialRegistro(string tabla, int registroId)
        {
            try
            {
                var historial = await _bitacoraService.ObtenerHistorialRegistroAsync(tabla, registroId);
                return Ok(new
                {
                    success = true,
                    data = historial,
                    message = $"Historial de {tabla} (ID: {registroId}) obtenido exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener historial: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtiene las bitácoras de un usuario específico
        /// </summary>
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerBitacorasUsuario(int usuarioId,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                var bitacoras = await _bitacoraService.ObtenerBitacorasPorUsuarioAsync(usuarioId, fechaInicio, fechaFin);
                return Ok(new
                {
                    success = true,
                    data = bitacoras,
                    total = bitacoras.Count,
                    message = "Bitácoras del usuario obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener bitácoras: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtiene las bitácoras recientes de los últimos N días
        /// </summary>
        [HttpGet("recientes")]
        public async Task<IActionResult> ObtenerBitacorasRecientes([FromQuery] int dias = 7)
        {
            try
            {
                if (dias < 1 || dias > 365)
                    return BadRequest(new { success = false, message = "Los días deben estar entre 1 y 365" });

                var bitacoras = await _bitacoraService.ObtenerBitacorasRecentesAsync(dias);
                return Ok(new
                {
                    success = true,
                    data = bitacoras,
                    total = bitacoras.Count,
                    dias = dias,
                    message = $"Bitácoras de los últimos {dias} días obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener bitácoras recientes: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtiene un resumen estadístico de la bitácora
        /// </summary>
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumenBitacora(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                // Si no se proporciona rango, usar los últimos 30 días
                fechaInicio = fechaInicio ?? DateTime.Now.AddDays(-30);
                fechaFin = fechaFin ?? DateTime.Now;

                var bitacoras = await _bitacoraService.ObtenerBitacorasRecentesAsync(
                    (int)(DateTime.Now - fechaInicio.Value).TotalDays);

                var resumen = bitacoras
                    .GroupBy(b => new { b.Tabla, b.Accion })
                    .Select(g => new
                    {
                        tabla = g.Key.Tabla,
                        accion = g.Key.Accion,
                        totalAcciones = g.Count(),
                        usuariosInvolucrados = g.Select(b => b.UsuarioId).Distinct().Count(),
                        registrosAfectados = g.Select(b => b.RegistroId).Distinct().Count(),
                        primeraAccion = g.Min(b => b.FechaAccion),
                        ultimaAccion = g.Max(b => b.FechaAccion)
                    })
                    .OrderBy(r => r.tabla)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = resumen,
                    totalRegistros = bitacoras.Count,
                    fechaInicio = fechaInicio.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    fechaFin = fechaFin.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    message = "Resumen de bitácora obtenido exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener resumen: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtiene todas las bitácoras (solo admin)
        /// </summary>
        [HttpGet("todas")]
        public async Task<IActionResult> ObtenerTodasBitacoras(
            [FromQuery] int dias = 30,
            [FromQuery] string? tabla = null,
            [FromQuery] string? accion = null)
        {
            try
            {
                var bitacoras = await _bitacoraService.ObtenerBitacorasRecentesAsync(dias);

                if (!string.IsNullOrEmpty(tabla))
                    bitacoras = bitacoras.Where(b => b.Tabla.Contains(tabla, StringComparison.OrdinalIgnoreCase)).ToList();

                if (!string.IsNullOrEmpty(accion))
                    bitacoras = bitacoras.Where(b => b.Accion.Contains(accion, StringComparison.OrdinalIgnoreCase)).ToList();

                // Calcular conteos por acción (usando Contains para más flexibilidad)
                var totalCrear = bitacoras.Count(b =>
                    b.Accion != null && (b.Accion.Contains("Registrar", StringComparison.OrdinalIgnoreCase) ||
                    b.Accion.Equals("Registrar", StringComparison.OrdinalIgnoreCase)));

                var totalActualizar = bitacoras.Count(b =>
                    b.Accion != null && (b.Accion.Contains("Actualizar", StringComparison.OrdinalIgnoreCase) ||
                    b.Accion.Equals("Actualizar", StringComparison.OrdinalIgnoreCase)));

                var totalEliminar = bitacoras.Count(b =>
                    b.Accion != null && (b.Accion.Contains("Eliminar", StringComparison.OrdinalIgnoreCase) ||
                    b.Accion.Equals("Eliminar", StringComparison.OrdinalIgnoreCase)));

                return Ok(new
                {
                    success = true,
                    data = bitacoras,
                    total = bitacoras.Count,
                    totalRegistrar = totalCrear,
                    totalActualizar = totalActualizar,
                    totalEliminar = totalEliminar,
                    filtros = new { dias, tabla, accion },
                    message = "Todas las bitácoras obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener bitácoras: {ex.Message}"
                });
            }
        }

        // Nuevo endpoint para filtrado avanzado y paginado desde frontend
        [HttpGet("filtrada")]
        public async Task<IActionResult> ObtenerBitacorasFiltrada(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] string? tabla = null,
            [FromQuery] string? accion = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Construir DTO de filtro (definido en los servicios)
                var filtro = new SistemaDonacion.DTOs.BitacoraFiltroDto
                {
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Tabla = tabla,
                    Accion = accion,
                    Pagina = pagina,
                    PageSize = pageSize
                };

                var resultado = await _bitacoraService.ObtenerBitacorasFiltradaAsync(filtro);

                return Ok(new
                {
                    success = true,
                    data = resultado.Data,
                    total = resultado.Total,
                    pagina = resultado.Pagina,
                    pageSize = resultado.PageSize,
                    totalPaginas = resultado.TotalPaginas,
                    fechaInicio = resultado.FechaInicio.ToString("yyyy-MM-dd HH:mm:ss"),
                    fechaFin = resultado.FechaFin.ToString("yyyy-MM-dd HH:mm:ss"),
                    totalRegistrar = resultado.TotalCrear,
                    totalActualizar = resultado.TotalActualizar,
                    totalEliminar = resultado.TotalEliminar,
                    message = "Bitácoras filtradas obtenidas exitosamente"
                });
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(new { success = false, message = argEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener bitácoras filtradas: {ex.Message}"
                });
            }
        }

        // Nuevo endpoint para obtener tablas y acciones disponibles
        [HttpGet("opciones")]
        public async Task<IActionResult> ObtenerOpcionesDisponibles()
        {
            try
            {
                var bitacoras = await _bitacoraService.ObtenerBitacorasRecentesAsync(365);

                var tablas = bitacoras
                    .Select(b => b.Tabla)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                var acciones = bitacoras
                    .Select(b => b.Accion)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    tablas = tablas,
                    acciones = acciones,
                    message = "Opciones obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener opciones: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Exporta registros de bitácora a Excel con los filtros aplicados
        /// </summary>
        [HttpPost("exportar")]
        public async Task<IActionResult> ExportarBitacoraExcel([FromBody] BitacoraExportDto filtros)
        {
            try
            {
                // Obtener registros según filtros
                List<BitacoraAccion> bitacoras = new List<BitacoraAccion>();

                if (!string.IsNullOrEmpty(filtros.FechaInicio) && !string.IsNullOrEmpty(filtros.FechaFin))
                {
                    // Usar filtrado avanzado con fechas personalizadas
                    var filtroDto = new BitacoraFiltroDto
                    {
                        FechaInicio = DateTime.TryParse(filtros.FechaInicio, out var fi) ? fi : DateTime.Now.AddDays(-30),
                        FechaFin = DateTime.TryParse(filtros.FechaFin, out var ff) ? ff : DateTime.Now,
                        Tabla = filtros.Tabla,
                        Accion = filtros.Accion,
                        Pagina = 1,
                        PageSize = 10000 // Máximo para exportación
                    };

                    var resultado = await _bitacoraService.ObtenerBitacorasFiltradaAsync(filtroDto);
                    // Convertir BitacoraResponseDto a BitacoraAccion
                    bitacoras = resultado.Data.Select(dto => new BitacoraAccion
                    {
                        Id = dto.Id,
                        UsuarioId = dto.UsuarioId,
                        Accion = dto.Accion,
                        Tabla = dto.Tabla,
                        RegistroId = dto.RegistroId,
                        DatosAnteriores = dto.DatosAnteriores,
                        DatosNuevos = dto.DatosNuevos,
                        Detalles = dto.Detalles,
                        IPAddress = dto.IPAddress,
                        DetallesCambios = dto.DetallesCambios,
                        FechaAccion = dto.FechaAccion,
                        Usuario = dto.Usuario
                    }).ToList();
                }
                else
                {
                    // Usar período en días
                    int dias = filtros.Dias > 0 ? filtros.Dias.Value : 30;
                    var allBitacoras = await _bitacoraService.ObtenerBitacorasRecentesAsync(dias);

                    // Aplicar filtros adicionales
                    if (!string.IsNullOrEmpty(filtros.Tabla))
                        allBitacoras = allBitacoras.Where(b => b.Tabla.Contains(filtros.Tabla, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (!string.IsNullOrEmpty(filtros.Accion))
                        allBitacoras = allBitacoras.Where(b => b.Accion.Contains(filtros.Accion, StringComparison.OrdinalIgnoreCase)).ToList();

                    bitacoras = allBitacoras.ToList();
                }

                if (bitacoras.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No hay registros para exportar con los filtros aplicados"
                    });
                }

                // Crear libro Excel
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Bitácora de Auditoría");

                    // Configurar encabezados
                    var headers = new[] { "ID", "Usuario", "Rol", "Tipo Acción", "Entidad", "ID Registro", "Fecha y Hora", "Descripción" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(102, 126, 234); // #667eea
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }

                    worksheet.Row(1).Height = 25;

                    // Agregar datos
                    int rowIndex = 2;
                    foreach (var bitacora in bitacoras)
                    {
                        // Mostrar alternancia de colores
                        var fillColor = rowIndex % 2 == 0 ? XLColor.White : XLColor.FromArgb(245, 245, 245); // #f5f5f5

                        worksheet.Cell(rowIndex, 1).Value = bitacora.Id;
                        worksheet.Cell(rowIndex, 2).Value = bitacora.Usuario?.Nombre ?? "Sistema";
                        worksheet.Cell(rowIndex, 3).Value = bitacora.Usuario?.Rol ?? "";
                        worksheet.Cell(rowIndex, 4).Value = FormatearAccion(bitacora.Accion ?? "Consulta");
                        worksheet.Cell(rowIndex, 5).Value = bitacora.Tabla ?? "";
                        worksheet.Cell(rowIndex, 6).Value = bitacora.RegistroId;
                        worksheet.Cell(rowIndex, 7).Value = bitacora.FechaAccion.ToString("dd/MM/yyyy HH:mm:ss");
                        worksheet.Cell(rowIndex, 8).Value = bitacora.Detalles ?? "";

                        // Aplicar estilos a la fila
                        for (int col = 1; col <= 8; col++)
                        {
                            var cell = worksheet.Cell(rowIndex, col);
                            cell.Style.Fill.BackgroundColor = fillColor;
                            cell.Style.Font.FontColor = XLColor.FromArgb(51, 51, 51); // #333
                            cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.TopBorderColor = XLColor.FromArgb(221, 221, 221); // #ddd
                            cell.Style.Border.BottomBorderColor = XLColor.FromArgb(221, 221, 221);
                            cell.Style.Border.LeftBorderColor = XLColor.FromArgb(221, 221, 221);
                            cell.Style.Border.RightBorderColor = XLColor.FromArgb(221, 221, 221);

                            // Alineación según columna
                            if (col == 6 || col == 7) // ID Registro y Fecha
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            else
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                            cell.Style.Alignment.WrapText = col == 8; // Envolver texto en Descripción
                        }

                        rowIndex++;
                    }

                    // Ajustar ancho de columnas
                    worksheet.Column(1).Width = 8;
                    worksheet.Column(2).Width = 15;
                    worksheet.Column(3).Width = 12;
                    worksheet.Column(4).Width = 15;
                    worksheet.Column(5).Width = 15;
                    worksheet.Column(6).Width = 12;
                    worksheet.Column(7).Width = 20;
                    worksheet.Column(8).Width = 30;

                    // Congelar encabezados
                    worksheet.SheetView.FreezeRows(1);

                    // Agregar metadatos en otra hoja
                    var metadataSheet = workbook.Worksheets.Add("Metadatos");
                    metadataSheet.Cell("A1").Value = "Información de Exportación";
                    metadataSheet.Cell("A1").Style.Font.Bold = true;
                    metadataSheet.Cell("A1").Style.Font.FontSize = 14;

                    metadataSheet.Cell("A3").Value = "Fecha de Exportación:";
                    metadataSheet.Cell("B3").Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    metadataSheet.Cell("A4").Value = "Usuario que exportó:";
                    var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Sistema";
                    metadataSheet.Cell("B4").Value = userName;

                    metadataSheet.Cell("A5").Value = "Total de Registros:";
                    metadataSheet.Cell("B5").Value = bitacoras.Count;

                    metadataSheet.Cell("A6").Value = "Período:";
                    var periodo = !string.IsNullOrEmpty(filtros.FechaInicio) && !string.IsNullOrEmpty(filtros.FechaFin)
                        ? $"Del {filtros.FechaInicio} al {filtros.FechaFin}"
                        : $"Últimos {(filtros.Dias > 0 ? filtros.Dias : 30)} días";
                    metadataSheet.Cell("B6").Value = periodo;

                    metadataSheet.Cell("A7").Value = "Filtros Aplicados:";
                    var filtrosAplicados = new List<string>();
                    if (!string.IsNullOrEmpty(filtros.Tabla)) filtrosAplicados.Add($"Entidad: {filtros.Tabla}");
                    if (!string.IsNullOrEmpty(filtros.Accion)) filtrosAplicados.Add($"Acción: {filtros.Accion}");
                    if (filtros.ExportarSoloResultadosVisibles) filtrosAplicados.Add("Solo resultados visibles");
                    metadataSheet.Cell("B7").Value = string.Join(", ", filtrosAplicados.Count > 0 ? filtrosAplicados : new List<string> { "Ninguno" });

                    metadataSheet.Column(1).Width = 25;
                    metadataSheet.Column(2).Width = 40;

                    // Generar nombre de archivo
                    string nombreArchivo = GenerarNombreArchivo(filtros);

                    // Guardar en memoria y descargar
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        return File(stream.ToArray(), 
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            nombreArchivo);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al exportar bitácora a Excel: {ex.Message}",
                    details = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Formatea el nombre de la acción para visualización
        /// </summary>
        private string FormatearAccion(string accion)
        {
            if (string.IsNullOrEmpty(accion)) return "Consulta";

            var textoLower = accion.ToLower();
            if (textoLower.Contains("crear") || textoLower.Contains("registrar")) return "Crear";
            if (textoLower.Contains("actualizar")) return "Actualizar";
            if (textoLower.Contains("eliminar")) return "Eliminar";
            if (textoLower.Contains("consulta") || textoLower.Contains("consultar")) return "Consulta";

            return accion;
        }

        /// <summary>
        /// Genera el nombre del archivo Excel con timestamp
        /// </summary>
        private string GenerarNombreArchivo(BitacoraExportDto filtros)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var hasFilters = !string.IsNullOrEmpty(filtros.Tabla) || !string.IsNullOrEmpty(filtros.Accion);

            if (hasFilters && !string.IsNullOrEmpty(filtros.Tabla))
                return $"bitacora_filtrada_{filtros.Tabla}_{timestamp}.xlsx";

            if (hasFilters)
                return $"bitacora_filtrada_{timestamp}.xlsx";

            return $"bitacora_auditoria_{timestamp}.xlsx";
        }
    }
}

// DTO para la solicitud de exportación
public class BitacoraExportDto
{
    public int? Dias { get; set; }
    public string? FechaInicio { get; set; }
    public string? FechaFin { get; set; }
    public string? Tabla { get; set; }
    public string? Accion { get; set; }
    public bool ExportarSoloResultadosVisibles { get; set; } = false;
}
