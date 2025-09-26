using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChafetzChesed.DAL.Entities
{
    public class SearchIndexItem
    {
        public int Id { get; set; }
        public int? InstitutionId { get; set; }
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string Keywords { get; set; } = "";
        public string Route { get; set; } = "/";
    }
}
