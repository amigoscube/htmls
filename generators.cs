using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

/// <summary>
/// Summary description for generators
/// </summary>
public class generators
{
	public generators()
	{
		//
		// TODO: Add constructor logic here
		//
	}

    [WebMethod]
    public static String hel() {
        return "from Cs File";
    }
}