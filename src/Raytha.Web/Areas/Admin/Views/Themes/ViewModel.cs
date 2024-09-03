using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Raytha.Application.Themes;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.Themes;

public class ThemesListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Title")]
    public string Title { get; init; }

    [Display(Name = "Developer Name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Description")]
    public string Description { get; init; }

    [Display(Name = "Last modified at:")]
    public string LastModificationTime { get; init; }

    [Display(Name = "Last modified by:")]
    public string LastModifierUser { get; init; }
}

public class ThemesCreate_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Title")]
    public string Title { get; init; }

    [Display(Name = "Description")]
    public string Description { get; init; }

    [Display(Name = "Developer Name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Insert default theme media items")]
    public bool InsertDefaultThemeMediaItems { get; set; } = true;
}

public class ThemesEdit_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }

    [Display(Name = "Title")]
    public string Title { get; init; }

    [Display(Name = "Developer Name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Description")]
    public string Description { get; init; }
}

public class ThemesActionsMenu_ViewModel
{
    public string ThemeId { get; set; }
}

public class ThemesBackToList_ViewModel
{
    public string ThemeId { get; set; }
    public bool IsWebTemplates { get; set; }
    public bool IsMediaItems { get; set; }
}

public class ThemesImportFromUrl_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Title")]
    public string Title { get; init; }

    [Display(Name = "Description")]
    public string Description { get; init; }

    [Display(Name = "Developer Name")]
    public string DeveloperName { get; set; }

    public string Url { get; set; }
}

public class ThemesExport_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }

    [Display(Name = "Is exportable")]
    public bool IsExportable { get; set; }

    [Display(Name = "Url")]
    public string Url { get; set; }
}

public class ThemesBackgroundTaskStatus_ViewModel 
{
    public string PathBase { get; set; }
}

public class ThemesMatchingWebTemplates_ViewModel
{
    public string ThemeId { get; set; }
    public IEnumerable<string> ActiveThemeWebTemplateDeveloperNames { get; set; }
    public IEnumerable<string> NewActiveThemeWebTemplateDeveloperNames { get; set; }
    public IDictionary<string, string> WebTemplateMappings { get; set; } = new Dictionary<string, string>();
}

public class ThemesDuplicate_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Theme")]
    public string ThemeId { get; init; }

    [Display(Name = "Title")]
    public string Title { get; init; }

    [Display(Name = "Description")]
    public string Description { get; init; }

    [Display(Name = "Developer Name")]
    public string DeveloperName { get; set; }

    public IEnumerable<ThemeDto> Themes { get; set; } = new List<ThemeDto>();
}