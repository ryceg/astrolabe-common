//#if (IncludeLocalUsers)
import { defaultUserRoutes } from "@astroapps/client-localusers";

export default {
  "": {
    label: "Home",
  },
  //#if (IncludeDemoData)
  tea: {
    label: "Tea Manager",
  },
  //#endif
  editor: {
    label: "Schema Editor",
  },
  ...defaultUserRoutes,
}
//#else
export default {
  "": {
    label: "Home",
  },
  //#if (IncludeDemoData)
  tea: {
    label: "Tea Manager",
  },
  //#endif
  editor: {
    label: "Schema Editor",
  },
}
//#endif
