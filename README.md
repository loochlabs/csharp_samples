# C# Code Sample

### Description
The following is a small code excerpt from a recent physics climbing game I made called Sweaty Palms (https://thecalooch.itch.io/sweaty-palms) These were the main player logic scripts in a larger Unity project.  

PlayerControls.cs  
Draggable.cs  

These were the main control scripts for the player during the game. The character featured a balance of joints and motors in order to naturally climb in an upward motion. In order to climb to the top, they needed to click and drag the hands and feet of the character. Draggable.cs served as the main controller for an individual foot or hand, with the player controlling four in total. PlayerControls.cs served as the main manager for the collection of Draggables and maintained the relationship between these elements. These scripts also feature support for a gamepad that required specific code to be written, such that clicking/dragging with a mouse is an entirely different UX from a gamepad.     

Several minor iteractions happened over the course of about three months of work. An initial prototype was built in a week where the user could control the hands and feet in order to climb upwards. However, it was very difficult to use. Small changes to the properties of the physics skeleton were constantly made in order for the climbing to be easier to control. With each change, it became simpler to control and climb upwards. Noticeable improvements could be seen when playtesting with others.

### Climbing in action
![alt text](https://alphabetagamer-3kkpqwvtmi.netdna-ssl.com/wp-content/uploads/2018/01/sweaty-palms-download.gif?x33728)

