using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EPA.Office365.Database
{
    public class ModelBase
    {
        [Key]
        public int ID { get; set; }
    }
}
