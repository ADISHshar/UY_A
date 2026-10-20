namespace TreasureHunt
{
    // Each leg must lift clear before it can produce another landing.
    public struct RobotFootContact
    {
        bool lifted;
        public bool Update(float soleHeight,bool moving)
        {
            if(!moving) { lifted=false; return false; }
            if(soleHeight>.025f) lifted=true;
            if(!lifted || soleHeight>.012f) return false;
            lifted=false;
            return true;
        }
    }
}
