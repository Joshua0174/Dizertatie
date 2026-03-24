using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Entities
{
    public enum UserRole
    {    
        SysAdmin,     //gestioneaza conturi pentru InstitutionAdmin
        InstitutionAdmin,   //gestioneaza conturi pentru officials
        Official, 
        Citizen
    }
}
