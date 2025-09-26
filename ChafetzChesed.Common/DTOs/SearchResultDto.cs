using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChafetzChesed.Common.DTOs
{
    public record SearchResultDto(
      string Type,        
      int? Id,         
      string Title,        
      string? Subtitle,    
      string? Snippet,     
      string Route,        
      int Score         
  );

}
