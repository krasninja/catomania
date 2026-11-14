# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- Upgrade to .NET 11.
- Add cat's new trick animation.
- Add Nolu colors.

## [0.4.1]

- Common drawing style for all animals: the cat, dog, pig, parrot, snake and panda now use the same near-black outlines as the artwork-based animals, and the cat, dog and pig get big glossy eyes, pink cheeks and (cat and dog) bigger heads.
- Muchi the Cat: a round-faced kitten in five fur colors (orange, gray, black, beige and brown), it glances left or right towards the cursor, walks and runs sideways wagging the tail, looks up with sparkling eyes, squints happily when jumping for joy and sleeps either curled up or on its back with the paws in the air, chosen at random each time it falls asleep. Its own "hunt" trick in pause mode: it crouches low, stalks, wiggles the rear and pounces.
- Nolu: a shaggy forest creature with deer antlers, it follows the cursor with its eyes, walks sideways wagging its fluffy tail, hunches over looking for mushrooms when running, nibbles a fly agaric, jumps for joy in its own happy trick and sleeps sitting with the arms folded.
- Update Capybara. Add capibara song.
- Add snake colors.
- The frog follows the cursor with its eyes in pause mode.
- Fix build for MacOS.
- Add "happy" trick.
- KDE context menu scrolling fix.
- Update cat, dog, parrot, pig design.

## [0.3.4]

- Pig: a rosy piglet with folded ears and a curled tail.
- Frog: a green frog that hops after the cursor and catches flies with its tongue.
- Capybara: a calm capybara with a sprout on its head, it waddles sideways and sleeps lying on its belly.
- Animal does not overlap the cursor. It goes nearyb.

## [0.3.0]

- Cursor tracking fixes.
- Autostart.
- Animal looks at cursor in pause mode.
- The application is published as a Native AOT binary: faster startup, a smaller download and no JIT.
- Snake: a chubby mint snake with a big head and a leaf on top, it scoots like an inchworm and sleeps curled into a ring.
- Parrot: a scarlet macaw with rainbow-banded wings and a big hooked beak, it waddles, runs in flapping hops and sleeps squatting.
- Fur colors for the dog (liver, black, red and lemon on white) and the parrot (scarlet, blue and gold, green and hyacinth macaws). The "Fur" menu shows the colors of the current animal, each animal remembers its own.
- Updates come from the latest GitHub release.
- Only one instance runs at a time, a second launch exits right away.
- Uninstalling on Windows removes the launch at startup entry.
- macOS: the app no longer shows in the Dock or the app switcher, it lives in the menu bar.
- Tricks in pause mode.
- "Website" and "About" menu items.

## [0.2.0]

- Add system tray icon.
- Animal animation fixes.
- Panda cub: a second animal with a big round head, stubby legs and a waddle, it dozes off sitting up. The "Animal" menu switches between the cat and the panda, the fur colors apply to the cat only.
- Dog: a liver and white springer spaniel drawn in profile with long legs, a bounding run with flying ears, it sleeps stretched out and paddles in its dreams.

## [0.1.0]

- Initial implementation: a cat that chases the mouse cursor across the screen.
