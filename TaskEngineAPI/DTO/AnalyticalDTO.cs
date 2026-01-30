namespace TaskEngineAPI.DTO
{
   
        public class AnalyticalDTO
        {
        public string? canalyticalname { get; set; }
        public string? canalyticalDescription { get; set; }
        public string? canalyticalprompt { get; set; }
        public string? capi_method { get; set; }
        public string? capi_url { get; set; }
        public string? capi_params { get; set; }
        public string? capi_headers { get; set; }
        public string? cbody { get; set; }
        public string? cbusiness_function { get; set; }
        public string? cstatus { get; set; }
        public string? csource_type { get; set; }
        public int? cdataset_id { get; set; }
        public string? crefresh_mode { get; set; }
        public string? ctime_range { get; set; }
        public bool? cdescriptive_analytics { get; set; }
        public bool? cpredictive_analytics { get; set; }
        public bool? cdiagnostic_analytics { get; set; }
        public bool? cprescriptive_analytics { get; set; }
        public string? canalysis_depth { get; set; }
        public string? cexplanation_tone { get; set; }
        public string? cauto_follow_up { get; set; }
        public int? nmax_row_limit { get; set; }
        public int? nquery_depth_limit { get; set; }
        public string? callowed_join_types { get; set; }
        public bool? cread_only_mode { get; set; }
        public string? caudit_logging { get; set; }
        public string? callowed_roles { get; set; }
        public string? cmasking_rule { get; set; }
        public string? cdefault_chart_type { get; set; }
        public string? ccolor_scheme { get; set; }
        public bool? cexport_excel { get; set; }
        public bool? cexport_pdf { get; set; }
        public bool? cexport_csv { get; set; }
        public bool? cexport_json { get; set; }
        public bool? cexport_png { get; set; }
        public bool? cenable_drill_down { get; set; }
        public bool? cshow_data_labels { get; set; }
        public bool? cenable_animations { get; set; }
        public string? ccolumn_mappings { get; set; }
        public bool? nis_active { get; set; }


    }


    public class GetAnalyticalDTO
    {
        public int? ID { get; set; }
        public int? ctenant_id { get; set; }
        public string? canalyticalname { get; set; }
        public string? canalyticalDescription { get; set; }
        public string? canalyticalprompt { get; set; }
        public string? capi_method { get; set; }
        public string? capi_url { get; set; }
        public string? capi_params { get; set; }
        public string? capi_headers { get; set; }
        public string? cbody { get; set; }
        public bool? nis_active { get; set; }
        public string? ccreated_by { get; set; }
        public DateTime? lcreated_date { get; set; }
        public string? cmodified_by { get; set; }
        public DateTime? lmodified_date { get; set; }  
    }


    public class AnalyticalResponse
    {
        //public bool Success { get; set; }
        public int TotalCount { get; set; }
        public List<GetAnalyticalDTO> Data { get; set; }
    }


}
