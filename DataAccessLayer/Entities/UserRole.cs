using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Entities
{
    public enum UserRole
    {
        Admin,   //gestioneaza conturi pentru officials
        Official, 
        Citizen
    }
}
