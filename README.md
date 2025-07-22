<div align="center">

  # 🕹️ Phase-Functioned Neural Network for Character Control in Godot

This project was developed as part of a Deep Learning course at **FCEN, University of Buenos Aires (UBA)**.  
Our goal was to integrate a **Phase-Functioned Neural Network (PFNN)** into the **Godot Engine** for real-time character animation.
</div>

> 🚧 **Status:** Incomplete – learning-focused academic project

## 🎯 Objective
Port the original PFNN project from Unity to Godot, using pretrained weights and biases, tweak the neural network with new animation data, and create a working system to animate a character via neural predictions and motion data.

## 📦 What We Did
✅ **Ported the PFNN architecture**  
✅ **Used original project’s weights and biases**  
✅ **Mapped Unity bone structure to Godot bones**  
✅ **Accessed and modified Godot bones directly**  
✅ **Interpolated missing bone data using `lerp()`**  
✅ **Converted coordinate systems between engines**


## ❌ What Didn't Work
- Bone transformations did **not behave correctly** in Godot  
- **Animations did not play as expected**  
- No **animation export** or **saving for retraining**  
- **Motion prediction** was incomplete or glitchy  
- PFNN outputs did not fully translate into valid skeletal poses


## 🧠 What We Learned
- Deep dive into PFNN structure and inference
- Hands-on with neural net weight application in a game engine
- Coordinate system mismatches between Unity and Godot
- Challenges of skeletal animation and retargeting
- Realities of integrating deep learning into game dev pipelines

## 🧪 Technologies
- **Godot Engine**
- **Unity Engine**
- **GDScript**
- **C#**
- **Python** (for weight processing)
- **Pretrained PFNN weights** from the original [Unity PFNN project](https://github.com/sebastianstarke/AI4Animation/tree/master/AI4Animation/SIGGRAPH_2017/TensorFlow/trained_Adam)

## 📚 Resources
- [Phase-Functioned Neural Networks for Character Control (Holden et al.)](https://theorangeduck.com/page/phase-functioned-neural-networks-character-control)
- [Original Unity-based PFNN](https://github.com/sebastianstarke/AI4Animation/tree/master/AI4Animation/SIGGRAPH_2017)
- [Godot Documentation](https://docs.godotengine.org/)

## 🔮 Next Steps (If Continued)
- Fix bone control issues in Godot
- Validate PFNN output directly with animation playback
- Enable animation export for retraining
- Replace lerped estimates with proper full skeleton data

