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


}
