# Human directional movement and camera turns

- Locked forward movement retains the existing Walk/Run blend tree.
- Locked sideways, backward and diagonal movement use separate in-place Human Movement clips on the existing Combat Footwork layer.
- Camera turns are separate one-shot animations: Sword Turn 1/2 while armed, Human Turn Left/Right while unarmed. They work with either camera lock mode, standing or moving.
- Unlocked reversal retains the authored Walk/Run Turn 180 behavior.
- Camera Turn Angle Threshold, Sword Turn Degrees Per Second, Idle Turn Speed and Turn Replay Delay are editable on PlayerCombatInput.
- No SampleScene, player save, vendor FBX or attack damage/event changes.

Validation: connected Unity Editor Play in an isolated temporary scene cloned from the authored player; directional clip selection and camera turn combinations exercised while armed/unarmed. Test assets are not shipped.
