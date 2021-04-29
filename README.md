
## Colored SDF in the Anime Rendering project

  - Shapes with a color --> decompose to lines which define 2 colors + 2 layers (one for each side)
    - Left side: red, layer 1 --> everything to the left on layer 1 is fully red, to the right we have a red SDF falloff
  - Use this https://www.shadertoy.com/view/llK3Wm 
  - Contour textures http://webstaff.itn.liu.se/~stegu/contourtextures/LSCtex-longer.pdf
  - Technically, I could get away with 3 stacked SDFs (since the 4th one is basically the "default" or "background")
  - What happens if I slightly shift one of the colored borders? (like, only shift the 2nd layer)
  - Remove green border
  - Generate SDF from colored texture
  - Higher res texture? Plus 3 more textures?
  - Sharpening texture?
  - Bezier Intersection https://stackoverflow.com/questions/27664298/calculating-intersection-point-of-quadratic-bezier-curve
  - https://iquilezles.org/www/articles/distfunctions2d/distfunctions2d.htm
  - https://www.iquilezles.org/www/articles/interiordistance/interiordistance.htm
  - https://www.shadertoy.com/view/3t33WH


## Tentacle Cat
- [Procedural animation](https://www.youtube.com/watch?v=e6Gjhr1IP6w)
- Inverse Kinematics with [Quaternion Cyclic Coordinate Descent](https://zalo.github.io/blog/inverse-kinematics/)
- Investigate [inverse dynamics](https://medium.com/unity3danimation/create-your-own-inverse-dynamics-in-unity-1ed0371ee658)
