using System;
using SnakeGame;
namespace Models
{
	public class Powerup
	{
        public int power { get; set; } //an int representing the powerup's unique ID
        public Vector2D loc { get; set; } //a Vector2D representing the location of the powerup
        public bool died { get; set; } //a bool indicating if the powerup "died"


        public Powerup(int power, Vector2D loc, bool died)
        {
            this.power = power;
            this.loc = loc;
            this.died = died;
        }
    }
}

