namespace FundedEdge.Web;

/// <summary>
/// Clase marcador para IStringLocalizer&lt;SharedResources&gt;. Convención de este proyecto: las
/// CLAVES son el texto literal en español (idioma por defecto) — si no hay recurso para la cultura
/// actual, el localizador devuelve la propia clave, así que el español no necesita .resx y no
/// puede haber claves sin traducir "a medias". El inglés vive en Resources/SharedResources.en.resx.
/// Para añadir un idioma nuevo: crear Resources/SharedResources.{cultura}.resx y añadir la cultura
/// a la lista de soportadas en Program.cs.
/// </summary>
public sealed class SharedResources;
