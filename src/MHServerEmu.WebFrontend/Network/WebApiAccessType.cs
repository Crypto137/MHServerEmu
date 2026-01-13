namespace MHServerEmu.WebFrontend.Network
{
    public enum WebApiAccessType
    {
        None,
        AccountManagement,

        /* 
         * Add more access types here as needed.
         * 
         * Do not change the order of existing types because they are saved to disk,
         * so changing the order can change access for previously saved keys.
         * 
         * If you want to add access types for your custom functionality, start with
         * a higher base value (e.g. 100000) to avoid conflicts with potential future
         * built-in access types.
         */

        NumTypes,
    }
}
