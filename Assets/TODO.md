# TODO: Diagnose Wind Effect on Bullets

## Tasks
- [x] Add debug logs in BallisticsManager.cs to log wind vector, velocity, and drag acceleration
- [x] Test the game and check console logs to see if wind is being sampled correctly
- [x] Verify WindConfig values in the asset
- [x] If wind is 0, check why (config null, windSpeed 0, etc.)
- [x] If wind is sampled but no effect, check if dragAcc is applied to velocity
- [x] Adjust wind speed or direction to amplify effect for testing
- [x] Assign WindManager to BallisticsManager's windManager field in the Unity Inspector
- [x] Diagnosed: Multiple BallisticsManager instances in scene; one has WindManager assigned, the firing one does not
- [ ] Identify the BallisticsManager on the object with DebugShoot
- [ ] Assign WindManager reference to that BallisticsManager in Inspector
- [ ] Remove duplicate BallisticsManager if not needed
- [ ] Test again to confirm wind effect
