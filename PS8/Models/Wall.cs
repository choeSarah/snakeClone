using System;
using System.Runtime.Serialization;
using SnakeGame;

namespace Models
{
    [DataContract(Namespace = "")]
    public class Wall
    {
        [DataMember(Name = "ID")]
        public int wall { get; set; } //an int representing the wall's unique ID
        [DataMember]
        public Vector2D p1 { get; set; } //a Vector2D representing one endpoint of the wall
        [DataMember]
        public Vector2D p2 { get; set; } //a Vector2D representing the other endpoint of the wall


        public Wall(int wall, Vector2D p1, Vector2D p2)
        {
            this.wall = wall;
            this.p1 = p1;
            this.p2 = p2;
        }
    }
}