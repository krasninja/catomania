using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// A calm capybara with a sprout on its head. It sits facing the viewer, turns sideways to waddle
/// on its short legs, lowers its head when running, munches a watermelon, sings its song twirling the booty
/// and sleeps lying on its belly.
/// </summary>
public sealed class Capybara : Animal
{
    // The capybara is drawn in the coordinates of its 800 x 800 artwork, the feet stand on the ground.
    // The artwork is scaled down a little, so the sprout stays in the design space at the top of a hop.
    private const double ArtScale = DesignSize / 870;
    private const double ArtCenterX = 400;
    private const double ArtGroundY = 684;

    // Horizontal shifts that center the side views in the design space, in the art coordinates.
    private const double SideViewShiftX = -34;
    private const double SleepingShiftX = -11;

    // The short legs take short steps, see Cat for how the stride is chosen. In the art coordinates.
    private const double WalkStanceFraction = 0.5;
    private const double RunStanceFraction = 0.35;
    private const double WalkReach = 36;
    private const double RunReach = 60;
    private const double WalkStride = 2 * WalkReach / WalkStanceFraction;
    private const double RunStride = 2 * RunReach / RunStanceFraction;

    // How far the face moves up when the capybara raises its head, in the art coordinates.
    private const double RaisedNostrilsShift = 20;
    private const double RaisedMouthShift = 40;

    // How far the head drops in the poses with the splayed feet, in the art coordinates.
    private const double PosingHeadDrop = 20;

    // Duration of a beat of the song in seconds, a relaxed capybara tempo.
    private const double SongBeatSeconds = 0.5;

    // The booty swings around a point high on the body, so the bottom moves the most, the head
    // tilts back a little to stay upright and bobs on every beat. In the art coordinates and degrees.
    private const double TwirlPivotX = 403;
    private const double TwirlPivotY = 300;
    private const double TwirlAngle = 14;
    private const double TwirlHeadTilt = 10;
    private const double TwirlHeadBob = 24;

    // Where the lowered head of the sleeping capybara goes on the running one, in the art coordinates.
    private static readonly Vector RunningHeadShift = new(14, -46);
    private static readonly Vector RunningSproutShift = new(-9.34, 3.152);

    // The song is sung note by note, and every note is one swing of the booty, to one side and then
    // to the other: first the long and slow "Ca-pi-baa-ra..." with the "baa" and the "ra" drawn out,
    // then four short and snappy "capibara".
    private static readonly SongNote[] Song =
    [
        new(1, "♪ Ca-", 0.6),
        new(1, "♪ Ca-pi-", 0.6),
        new(1, "♪ Ca-pi-baa-", 0.6),
        new(1, "♪ Ca-pi-baa-ra...", 0.6),
        new(.8, "capibara ♪", 1),
        new(.8, "♪ capibara", 1),
        new(.8, "capibara ♪", 1),
        new(.8, "♪ capibara!", 1),
        new(.8, "capibara! ♪", 1),
    ];

    private static readonly double SongBeats = Song.Sum(note => note.Beats);

    private static readonly Color FurColor = Color.FromUInt32(0xFFE4A168);
    private static readonly Color PupilColor = Color.FromUInt32(0xFF000000);

    private static readonly Matrix ArtToDesign = Matrix.CreateScale(ArtScale, ArtScale)
        * Matrix.CreateTranslation((DesignSize / 2) - (ArtCenterX * ArtScale), GroundY - (ArtGroundY * ArtScale));

    private static readonly IBrush Fur = new SolidColorBrush(FurColor);
    private static readonly IBrush FurDark = new SolidColorBrush(Color.FromUInt32(0xFFBB7D51));
    private static readonly IBrush Leaf = new SolidColorBrush(Color.FromUInt32(0xFFBDE28F));
    private static readonly IBrush Cheek = new SolidColorBrush(Color.FromUInt32(0xFFEFA392));
    private static readonly IBrush Outline = new SolidColorBrush(PupilColor);
    private static readonly IBrush MelonRindBrush = new SolidColorBrush(Color.FromUInt32(0xFF007C0C));
    private static readonly IBrush MelonWhiteBrush = new SolidColorBrush(Color.FromUInt32(0xFFF6DBB7));
    private static readonly IBrush MelonPulpBrush = new SolidColorBrush(Color.FromUInt32(0xFFD8001D));
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));

    // Drawn under the fill, so only the outer half of the stroke is visible.
    private static readonly IPen OutlinePen = new Pen(Outline, 24, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen FacePen = new Pen(Outline, 12, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen HappyEyePen = new Pen(Outline, 18, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen ZzzPen = new Pen(
        new SolidColorBrush(Color.FromUInt32(0xFFC9D6E3)), 1.8, null, PenLineCap.Round, PenLineJoin.Round);

    private static readonly Geometry LeftFoot = Parse(
        """
        M318.133 662.701C324.137 651.887 305.635 642.788 305.635 642.788C305.635 642.788 329.87 538.507
        262.928 553.384C189.955 569.601 256.708 656.191 256.708 656.191C256.708 656.191 245.782 673.853
        260.365 679.316C271.655 683.546 275.597 673.606 277.028 669.287C280.975 678.624 287.913 683.812
        295.67 679.976C303.427 676.141 300.224 662.983 300.224 662.983C300.224 662.983 312.13 673.516
        318.133 662.701Z
        """);

    private static readonly Geometry RightFoot = Parse(
        """
        M489.453 662.701C483.45 651.887 501.952 642.788 501.952 642.788C501.952 642.788 477.716 538.507
        544.659 553.384C617.631 569.601 550.878 656.191 550.878 656.191C550.878 656.191 561.805 673.853
        547.222 679.316C535.932 683.546 531.989 673.606 530.558 669.287C526.611 678.624 519.673 683.812
        511.916 679.976C504.159 676.141 507.363 662.983 507.363 662.983C507.363 662.983 495.457 673.516
        489.453 662.701Z
        """);

    private static readonly Geometry Body = Parse(
        """
        M403.5 322C508.158 322 593 421.864 593 511.224C593 600.584 508.158 645.601 403.5
        645.601C298.842 645.601 214 600.584 214 511.224C214 421.864 298.842 322 403.5 322Z
        """);

    private static readonly Geometry FrontLeftFoot = Parse(
        """
        M353.453 681.956C362.051 673.064 346.534 659.486 346.534 659.486C346.534 659.486 396.934
        565.032 328.422 562.076C253.739 558.853 295.806 659.769 295.806 659.769C295.806 659.769 280.68
        674.001 293.352 683.053C303.163 690.061 309.544 681.48 312.044 677.678C313.44 687.719 318.799
        694.525 327.284 692.828C335.77 691.131 336.081 677.592 336.081 677.592C336.081 677.592 344.855
        690.848 353.453 681.956Z
        """);

    private static readonly Geometry FrontRightFoot = Parse(
        """
        M446.945 681.956C438.347 673.064 453.864 659.486 453.864 659.486C453.864 659.486 403.464
        565.032 471.976 562.076C546.659 558.853 504.592 659.769 504.592 659.769C504.592 659.769 519.717
        674.001 507.046 683.053C497.235 690.061 490.854 681.48 488.354 677.678C486.958 687.719 481.599
        694.525 473.114 692.828C464.628 691.131 464.317 677.592 464.317 677.592C464.317 677.592 455.543
        690.848 446.945 681.956Z
        """);

    private static readonly Geometry Sprout = Parse(
        """
        M400.5 68.9502C411.415 51.5653 442.949 47.7695 452.615 60.6482C460.782 71.5312 453.193 84.5266
        440.285 87.1502C428.702 89.5044 403.923 73.2656 403.864 73.2127C404.455 90.475 412.272 126
        412.272 126H388.727C388.727 126 396.544 90.475 397.135 73.2127C397.135 73.2127 372.27 89.4984
        360.716 87.1502C347.808 84.5268 340.218 71.5312 348.385 60.6482C358.051 47.7695 389.586 51.5653
        400.5 68.9502Z
        """);

    private static readonly Geometry Head = Parse(
        """
        M502.355 485.625C582.84 485.625 641.273 409.062 620.038 331.429C607.353 285.052 616.355 242.69
        587.626 196.934C511.243 75.2821 296.185 75.2825 219.804 196.935C191.075 242.69 200.162 284.745
        187.392 331.429C166.157 409.062 224.59 485.625 305.075 485.625H502.355Z
        """);

    private static readonly Geometry NosePatch = Parse(
        """
        M311 319C311 272.608 348.608 235 395 235H413C459.392 235 497 272.608 497 319V357C497 392.898
        467.899 422 432 422H376C340.101 422 311 392.899 311 357V319Z
        """);

    private static readonly Geometry Mouth = Parse(
        """
        M404 360C404 363.978 401.788 367.794 397.849 370.607C393.911 373.42 388.57 375 383 375M404
        360C404 363.978 406.212 367.794 410.151 370.607C414.089 373.42 419.43 375 425 375M404 360V321
        """);

    private static readonly Geometry Nostrils = Parse(
        """
        M459.444 295.495C461.515 299.082 458.541 304.677 452.801 307.99C447.062 311.304 440.73 311.082
        438.659 307.495C436.588 303.908 439.562 298.314 445.301 295C451.041 291.686 457.372 291.908
        459.444 295.495Z M368.444 307.495C366.372 311.082 360.041 311.304 354.301 307.99C348.562
        304.677 345.588 299.082 347.659 295.495C349.73 291.908 356.062 291.686 361.801 295C367.541
        298.314 370.515 303.908 368.444 307.495Z
        """);

    private static readonly Geometry EarInners = Parse(
        """
        F0 M250.882 189.589C239.056 185.098 233.075 175.471 234.232 162.247C235.628 146.292 249.694
        134.489 265.65 135.885C278.873 137.042 287.316 144.604 289.685 157.029C275.071 165.8 262.057
        176.72 250.882 189.589Z M557.071 189.589C568.896 185.098 574.877 175.471 573.72 162.247C572.324
        146.292 558.259 134.489 542.303 135.885C529.08 137.042 520.637 144.604 518.268 157.029C532.882
        165.8 545.896 176.72 557.071 189.589Z
        """);

    private static readonly Geometry EarRims = Parse(
        """
        F0 M301.7 150.863C296.442 136.309 283.126 125.368 266.695 123.93C244.138 121.957 224.252
        138.643 222.278 161.201C220.841 177.631 229.299 192.648 242.719 200.354C242.719 200.354 247.694
        193.793 250.882 189.589C239.056 185.098 233.075 175.471 234.232 162.247C235.628 146.292 249.694
        134.489 265.65 135.885C278.873 137.042 287.316 144.604 289.685 157.029C294.377 154.621 301.7
        150.863 301.7 150.863Z M506.252 150.863C511.511 136.309 524.827 125.368 541.257 123.93C563.815
        121.957 583.701 138.643 585.675 161.201C587.112 177.631 578.654 192.648 565.234 200.354C565.234
        200.354 560.259 193.793 557.071 189.589C568.896 185.098 574.877 175.471 573.72 162.247C572.324
        146.292 558.259 134.489 542.303 135.885C529.08 137.042 520.637 144.604 518.268 157.029C513.575
        154.621 506.252 150.863 506.252 150.863Z
        """);

    private static readonly Geometry SplayedLeftFoot = Parse(
        """
        M250.982 687.932C263.35 687.724 261.978 667.152 261.978 667.152C261.978 667.152 364.406 636
        318.051 585.464C267.521 530.377 225.908 631.481 225.908 631.481C225.908 631.481 205.149 630.85
        207.709 646.211C209.691 658.103 220.271 656.547 224.726 655.627C218.614 663.714 217.59 672.316
        224.79 677.116C231.99 681.916 241.784 672.563 241.784 672.563C241.784 672.563 238.615 688.14
        250.982 687.932Z
        """);

    private static readonly Geometry SplayedRightFoot = Parse(
        """
        M550.539 687.932C538.172 687.724 539.543 667.152 539.543 667.152C539.543 667.152 437.116 636
        483.471 585.464C534.001 530.377 575.614 631.481 575.614 631.481C575.614 631.481 596.372 630.85
        593.812 646.211C591.83 658.103 581.251 656.547 576.795 655.627C582.908 663.714 583.931 672.316
        576.731 677.116C569.531 681.916 559.738 672.563 559.738 672.563C559.738 672.563 562.907 688.14
        550.539 687.932Z
        """);

    private static readonly Geometry CuteLeftPaw = Parse(
        """
        M368.126 633.358C371.126 621.358 350.899 617.358 350.899 617.358C350.899 617.358 347.319
        510.358 286.508 542.054C220.22 576.604 307.109 642.967 307.109 642.967C307.109 642.967 301.126
        662.855 316.626 664.358C328.626 665.521 329.861 654.9 330.126 650.358C336.355 658.355 344.399
        661.57 350.899 655.858C357.399 650.145 350.899 638.265 350.899 638.265C350.899 638.265 365.126
        645.358 368.126 633.358Z
        """);

    private static readonly Geometry CuteRightPaw = Parse(
        """
        M434.215 633.357C431.215 621.357 451.441 617.357 451.441 617.357C451.441 617.357 455.022
        510.358 515.833 542.053C582.121 576.604 495.232 642.967 495.232 642.967C495.232 642.967 501.215
        662.855 485.715 664.358C473.715 665.521 472.479 654.899 472.215 650.357C465.985 658.355 457.941
        661.57 451.441 655.857C444.941 650.145 451.441 638.265 451.441 638.265C451.441 638.265 437.215
        645.357 434.215 633.357Z
        """);

    private static readonly Geometry EatLeftPaw = Parse(
        """
        M310.457 415.113C298.564 418.515 305.213 438.032 305.213 438.032C305.213 438.032 214.339
        494.632 272.194 531.448C335.26 571.58 349.287 463.151 349.287 463.151C349.287 463.151 369.502
        458.388 363.053 444.213C358.061 433.239 348.245 437.48 344.179 439.522C347.99 430.128 346.752
        421.555 338.555 418.782C330.358 416.009 323.319 427.578 323.319 427.578C323.319 427.578 322.349
        411.711 310.457 415.113Z
        """);

    private static readonly Geometry EatRightPaw = Parse(
        """
        M496.884 415.113C508.777 418.515 502.127 438.032 502.127 438.032C502.127 438.032 593.002
        494.632 535.147 531.448C472.081 571.58 458.054 463.151 458.054 463.151C458.054 463.151 437.839
        458.388 444.287 444.213C449.28 433.239 459.096 437.48 463.162 439.522C459.351 430.128 460.588
        421.555 468.785 418.782C476.982 416.009 484.021 427.578 484.021 427.578C484.021 427.578 484.992
        411.711 496.884 415.113Z
        """);

    private static readonly Geometry MelonRind = Parse(
        """
        M542.33 334C542.33 406.902 483.232 466 410.33 466C337.429 466 278.33 406.902 278.33 334L410.33
        334L542.33 334Z
        """);

    private static readonly Geometry MelonWhite = Parse(
        """
        M532.406 334C532.406 401.42 477.751 456.075 410.33 456.075C342.91 456.075 288.255 401.42
        288.255 334L410.33 334L532.406 334Z
        """);

    private static readonly Geometry MelonPulp = Parse(
        """
        M515.203 334C515.203 391.92 468.25 438.873 410.331 438.873C352.411 438.873 305.459 391.92
        305.459 334L410.33 334L515.203 334Z
        """);

    private static readonly Geometry MelonSeeds = Parse(
        """
        M416.616 420.727C416.616 424.171 413.802 426.963 410.331 426.963C406.859 426.963 404.045
        424.171 404.045 420.727C404.045 417.282 410.331 407.775 410.331 407.775C410.331 407.775 416.616
        417.282 416.616 420.727Z M464.439 397.083C466.874 399.519 466.858 403.483 464.404
        405.938C461.949 408.392 457.985 408.408 455.549 405.973C453.114 403.537 450.836 392.37 450.836
        392.37C450.836 392.37 462.003 394.648 464.439 397.083Z M356.223 397.083C353.788 399.519 353.803
        403.483 356.258 405.938C358.713 408.392 362.677 408.408 365.112 405.973C367.548 403.537 369.826
        392.37 369.826 392.37C369.826 392.37 358.658 394.648 356.223 397.083Z M489.117 356.827C492.561
        356.827 495.353 359.642 495.353 363.113C495.353 366.585 492.561 369.399 489.117 369.399C485.673
        369.399 476.165 363.113 476.165 363.113C476.165 363.113 485.673 356.827 489.117 356.827Z
        M331.544 356.827C328.1 356.827 325.308 359.642 325.308 363.113C325.308 366.585 328.1 369.399
        331.544 369.399C334.988 369.399 344.496 363.113 344.496 363.113C344.496 363.113 334.988 356.827
        331.544 356.827Z M454.961 376.837C457.944 378.559 461.769 377.518 463.504 374.512C465.24
        371.505 464.229 367.672 461.247 365.95C458.264 364.228 446.887 364.918 446.887 364.918C446.887
        364.918 451.978 375.115 454.961 376.837Z M365.701 376.837C362.718 378.559 358.893 377.518
        357.157 374.512C355.421 371.505 356.432 367.672 359.415 365.95C362.398 364.228 373.774 364.918
        373.774 364.918C373.774 364.918 368.683 375.115 365.701 376.837Z M419.969 392.441C420.86
        395.768 424.301 397.736 427.654 396.838C431.007 395.939 433.003 392.514 432.112 389.188C431.22
        385.861 422.688 378.304 422.688 378.304C422.688 378.304 419.077 389.114 419.969 392.441Z
        M400.693 392.441C399.801 395.768 396.361 397.736 393.007 396.838C389.654 395.939 387.658
        392.514 388.55 389.187C389.441 385.861 397.974 378.304 397.974 378.304C397.974 378.304 401.584
        389.114 400.693 392.441Z
        """);

    private static readonly Geometry HappyEyes = Parse(
        """
        M479 228.176C490.967 224.038 499.172 223.746 511.403 227.023C523.633 230.301 530.593 234.656
        538.888 244.223 M329.346 228.176C317.379 224.038 309.174 223.746 296.944 227.023C284.713
        230.301 277.753 234.656 269.459 244.223
        """);

    private static readonly Geometry FarHindLeg = Parse(
        """
        M276.945 646.956C268.347 638.064 283.864 624.486 283.864 624.486C283.864 624.486 233.464
        530.032 301.976 527.076C376.659 523.853 334.592 624.769 334.592 624.769C334.592 624.769 349.717
        639.001 337.046 648.053C327.235 655.061 320.854 646.48 318.354 642.678C316.958 652.719 311.599
        659.525 303.114 657.828C294.628 656.131 294.317 642.592 294.317 642.592C294.317 642.592 285.543
        655.848 276.945 646.956Z
        """);

    private static readonly Geometry FarFrontLeg = Parse(
        """
        M489.539 646.956C480.941 638.064 496.458 624.486 496.458 624.486C496.458 624.486 446.058
        530.032 514.57 527.076C589.253 523.853 547.187 624.769 547.187 624.769C547.187 624.769 562.312
        639.001 549.64 648.053C539.829 655.061 533.448 646.48 530.948 642.678C529.552 652.719 524.193
        659.525 515.708 657.828C507.223 656.131 506.911 642.592 506.911 642.592C506.911 642.592 498.137
        655.848 489.539 646.956Z
        """);

    private static readonly Geometry SideBody = Parse(
        """
        M311 298C433.607 298 583 357.988 583 447.458C583 536.928 473.607 602 351 602C228.393 602 129
        586.928 129 497.458C129 407.988 188.393 298 311 298Z
        """);

    private static readonly Geometry NearHindLeg = Parse(
        """
        M240.453 671.956C249.051 663.064 233.534 649.486 233.534 649.486C233.534 649.486 283.934
        555.032 215.422 552.076C140.739 548.853 182.806 649.769 182.806 649.769C182.806 649.769 167.68
        664.001 180.352 673.053C190.163 680.061 196.544 671.48 199.044 667.678C200.44 677.719 205.799
        684.525 214.284 682.828C222.77 681.131 223.081 667.592 223.081 667.592C223.081 667.592 231.855
        680.848 240.453 671.956Z
        """);

    private static readonly Geometry NearFrontLeg = Parse(
        """
        M451.225 671.956C459.823 663.064 444.306 649.486 444.306 649.486C444.306 649.486 494.706
        555.032 426.194 552.076C351.511 548.853 393.578 649.769 393.578 649.769C393.578 649.769 378.452
        664.001 391.124 673.053C400.935 680.061 407.316 671.48 409.816 667.678C411.212 677.719 416.571
        684.525 425.056 682.828C433.542 681.131 433.853 667.592 433.853 667.592C433.853 667.592 442.627
        680.848 451.225 671.956Z
        """);

    private static readonly Geometry SideSprout = Parse(
        """
        M534.5 113.95C545.415 96.5653 576.949 92.7695 586.615 105.648C594.782 116.531 587.193 129.527
        574.285 132.15C562.702 134.504 537.923 118.266 537.864 118.213C538.455 135.475 546.272 171
        546.272 171H522.727C522.727 171 530.544 135.475 531.135 118.213C531.135 118.213 506.27 134.498
        494.716 132.15C481.808 129.527 474.218 116.531 482.385 105.648C492.051 92.7695 523.586 96.5653
        534.5 113.95Z
        """);

    private static readonly Geometry SideFarEarInner = Parse(
        """
        F0 M656.738 234.589C668.563 230.098 674.544 220.471 673.387 207.247C671.991 191.292 657.925
        179.489 641.97 180.885C628.746 182.042 620.303 189.604 617.934 202.029C632.549 210.8 645.562
        221.72 656.738 234.589Z
        """);

    private static readonly Geometry SideFarEarRim = Parse(
        """
        F0 M605.919 195.863C611.178 181.309 624.494 170.368 640.924 168.93C663.482 166.957 683.368
        183.643 685.341 206.201C686.779 222.631 678.321 237.648 664.9 245.354C664.9 245.354 659.925
        238.793 656.738 234.589C668.563 230.098 674.544 220.471 673.387 207.247C671.991 191.292 657.925
        179.489 641.97 180.885C628.746 182.042 620.303 189.604 617.934 202.029C613.242 199.621 605.919
        195.863 605.919 195.863Z
        """);

    private static readonly Geometry SideHead = Parse(
        """
        M599.355 530.625C679.84 530.625 738.273 454.062 717.038 376.429C704.353 330.052 713.355 287.69
        684.626 241.934C608.243 120.282 433.185 120.283 356.804 241.935C328.075 287.69 337.162 329.745
        324.392 376.429C303.157 454.062 361.59 530.625 442.075 530.625H599.355Z
        """);

    private static readonly Geometry SideNosePatch = Parse(
        """
        M500 364C500 317.608 537.608 280 584 280H602C648.392 280 686 317.608 686 364V402C686 437.898
        656.899 467 621 467H565C529.101 467 500 437.899 500 402V364Z
        """);

    private static readonly Geometry SideMouth = Parse(
        """
        M616 405C616 408.978 613.788 412.794 609.849 415.607C605.911 418.42 600.57 420 595 420M616
        405C616 408.978 618.212 412.794 622.151 415.607C626.089 418.42 631.43 420 637 420M616 405V366
        """);

    private static readonly Geometry SideNostrils = Parse(
        """
        M671.444 340.495C673.515 344.082 670.541 349.677 664.801 352.99C659.062 356.304 652.73 356.082
        650.659 352.495C648.588 348.908 651.562 343.314 657.301 340C663.041 336.686 669.372 336.908
        671.444 340.495Z M580.444 352.495C578.372 356.082 572.041 356.304 566.301 352.99C560.562
        349.677 557.588 344.082 559.659 340.495C561.73 336.908 568.062 336.686 573.801 340C579.541
        343.314 582.515 348.908 580.444 352.495Z
        """);

    private static readonly Geometry SideNearEarInner = Parse(
        """
        F0 M399.525 234.589C387.7 230.098 381.719 220.471 382.876 207.247C384.272 191.292 398.337
        179.489 414.293 180.885C427.516 182.042 435.959 189.604 438.328 202.029C423.714 210.8 410.7
        221.72 399.525 234.589Z
        """);

    private static readonly Geometry SideNearEarRim = Parse(
        """
        F0 M450.344 195.863C445.085 181.309 431.769 170.368 415.339 168.93C392.781 166.957 372.895
        183.643 370.921 206.201C369.484 222.631 377.942 237.648 391.362 245.354C391.362 245.354 396.337
        238.793 399.525 234.589C387.7 230.098 381.719 220.471 382.876 207.247C384.272 191.292 398.337
        179.489 414.293 180.885C427.516 182.042 435.959 189.604 438.328 202.029C443.021 199.621 450.344
        195.863 450.344 195.863Z
        """);

    private static readonly Geometry SleepingFarPaw = Parse(
        """
        M620.702 672.856C609.887 678.859 600.788 660.357 600.788 660.357C600.788 660.357 496.508
        684.593 511.384 617.65C527.601 544.678 614.191 611.43 614.191 611.43C614.191 611.43 631.853
        600.504 637.316 615.087C641.546 626.377 631.606 630.32 627.287 631.751C636.624 635.698 641.812
        642.636 637.976 650.393C634.141 658.15 620.983 654.946 620.983 654.946C620.983 654.946 631.516
        666.852 620.702 672.856Z
        """);

    private static readonly Geometry SleepingBody = Parse(
        """
        M288 359.723C410.607 359.723 560 419.71 560 509.18C560 598.65 460.607 643.723 338
        643.723C215.393 643.723 116 628.65 116 539.18C116 449.71 165.393 359.723 288 359.723Z
        """);

    private static readonly Geometry SleepingNearPaw = Parse(
        """
        M519.844 616.683C513.48 606.077 496.35 617.551 496.35 617.551C496.35 617.551 418.158 544.422
        397.57 609.834C375.128 681.138 483.494 666.624 483.494 666.624C483.494 666.624 493.326 684.917
        505.349 675.02C514.657 667.357 508.02 658.973 504.995 655.574C515.055 656.824 523.016 653.41
        523.573 644.774C524.13 636.139 511.133 632.334 511.133 632.334C511.133 632.334 526.208 627.29
        519.844 616.683Z
        """);

    private static readonly Geometry SleepingHindLeg = Parse(
        """
        M249.844 616.683C243.48 606.077 226.35 617.551 226.35 617.551C226.35 617.551 148.158 544.422
        127.57 609.834C105.128 681.138 213.494 666.624 213.494 666.624C213.494 666.624 223.326 684.917
        235.349 675.02C244.657 667.357 238.02 658.973 234.995 655.574C245.055 656.824 253.016 653.41
        253.573 644.774C254.13 636.139 241.133 632.334 241.133 632.334C241.133 632.334 256.208 627.29
        249.844 616.683Z
        """);

    private static readonly Geometry SleepingSprout = Parse(
        """
        M598.375 180.771C613.417 166.804 644.859 171.299 650.862 186.24C655.935 198.867 645.24 209.455
        632.093 208.648C620.296 207.924 600.564 185.826 600.521 185.759C596.624 202.586 594.98 238.924
        594.98 238.924L572.237 232.83C572.237 232.83 588.982 200.539 594.021 184.018C594.021 184.018
        565.788 193.313 555.235 188.054C543.446 182.179 539.478 167.662 550.184 159.264C562.854 149.326
        592.332 161.154 598.375 180.771Z
        """);

    private static readonly Geometry SleepingFarEarInner = Parse(
        """
        F0 M675.884 332.089C688.468 330.812 696.737 323.06 699.042 309.988C701.824 294.215 691.292
        279.174 675.519 276.393C662.446 274.087 652.334 279.206 646.83 290.596C658.676 302.85 668.42
        316.766 675.884 332.089Z
        """);

    private static readonly Geometry SleepingFarEarRim = Parse(
        """
        F0 M636.82 281.53C645.666 268.833 661.36 261.711 677.602 264.575C699.902 268.507 714.792
        289.772 710.86 312.072C707.996 328.314 695.939 340.63 680.982 344.6C680.982 344.6 677.875
        336.975 675.884 332.089C688.468 330.812 696.737 323.06 699.042 309.988C701.824 294.215 691.292
        279.174 675.519 276.393C662.446 274.087 652.334 279.206 646.83 290.596C642.92 287.055 636.82
        281.53 636.82 281.53Z
        """);

    private static readonly Geometry SleepingHead = Parse(
        """
        M543.836 603.186C621.579 624.017 697.836 565.187 697.418 484.703C697.168 436.622 716.828
        398.034 700.92 346.402C658.626 209.126 489.533 163.818 384.268 261.556C344.676 298.317 342.568
        341.29 318.152 383.079C277.547 452.571 314.173 541.648 391.915 562.479L543.836 603.186Z
        """);

    private static readonly Geometry SleepingNosePatch = Parse(
        """
        M490.992 416.524C502.999 371.713 549.06 345.12 593.871 357.127L611.258 361.786C656.069 373.793
        682.662 419.853 670.655 464.664L660.819 501.369C651.528 536.045 615.886 556.622 581.211
        547.331L527.119 532.837C492.444 523.546 471.866 487.904 481.157 453.229L490.992 416.524Z
        """);

    private static readonly Geometry SleepingMouth = Parse(
        """
        M592.428 486.15C591.398 489.992 588.274 493.105 583.742 494.803C579.21 496.501 573.641 496.645
        568.261 495.203M592.428 486.15C591.398 489.992 592.548 494.25 595.624 497.987C598.7 501.723
        603.45 504.632 608.83 506.074M592.428 486.15L602.522 448.479
        """);

    private static readonly Geometry SleepingNostrils = Parse(
        """
        M662.677 438.193C663.75 442.194 659.429 446.828 653.027 448.543C646.626 450.258 640.567 448.405
        639.495 444.404C638.423 440.403 642.744 435.769 649.145 434.054C655.547 432.339 661.605 434.192
        662.677 438.193Z M571.672 426.231C568.743 429.16 562.57 427.736 557.884 423.049C553.197 418.363
        551.773 412.19 554.702 409.261C557.631 406.332 563.804 407.756 568.49 412.443C573.177 417.129
        574.601 423.302 571.672 426.231Z
        """);

    private static readonly Geometry SleepingNearEarInner = Parse(
        """
        F0 M427.435 265.517C417.175 258.119 413.889 247.272 418.43 234.798C423.907 219.748 440.549
        211.988 455.599 217.465C468.073 222.005 474.271 231.495 473.343 244.11C456.957 248.8 441.56
        255.979 427.435 265.517Z
        """);

    private static readonly Geometry SleepingNearEarRim = Parse(
        """
        F0 M486.545 241.264C485.232 225.845 475.202 211.83 459.703 206.189C438.425 198.444 414.898
        209.416 407.153 230.694C401.512 246.192 405.796 262.887 416.764 273.804C416.764 273.804 423.268
        268.753 427.435 265.517C417.175 258.119 413.889 247.272 418.43 234.798C423.907 219.748 440.549
        211.988 455.599 217.465C468.073 222.005 474.271 231.495 473.343 244.11C478.499 242.999 486.545
        241.264 486.545 241.264Z
        """);

    private static readonly Point[] EyeCenters = [new(282, 258), new(525, 258)];
    private static readonly Point[] CuteEyeCenters = [new(287, 236), new(512, 236)];
    private static readonly Point[] SideEyeCenters = [new(475, 302), new(663, 302)];

    // The eyes of the running capybara are in the coordinates of the sleeping head.
    private static readonly Point[] RunningEyeCenters = [new(482.891, 350.166), new(664.485, 398.824)];

    private enum Face
    {
        Idle,
        LookAhead,
        Blink,
        Happy,
        Cute,
        Eat,
        Startled,
    }

    /// <inheritdoc />
    public override AnimalKind Kind => AnimalKind.Capybara;

    /// <inheritdoc />
    public override Color BodyColor => FurColor;

    /// <inheritdoc />
    public override Color EyeColor => PupilColor;

    /// <inheritdoc />
    public override string Sound => "Squeak!";

    /// <inheritdoc />
    public override AnimalSimulatorOptions CreateSimulatorOptions() => new()
    {
        WalkStrideLength = WalkStride * ArtScale / DesignSize,
        RunStrideLength = RunStride * ArtScale / DesignSize,
        Tricks = [.. new AnimalSimulatorOptions().Tricks, AnimalTrick.CapybaraSong],
        TrickDurations = new Dictionary<AnimalTrick, double>
        {
            [AnimalTrick.CapybaraSong] = SongBeats * SongBeatSeconds,
        },
    };

    /// <inheritdoc />
    public override string? GetTrickSpeech(AnimalRenderState state)
    {
        if (state.Trick != AnimalTrick.CapybaraSong)
        {
            return null;
        }

        return Song[FindSongNote(state.TrickProgress).Index].Text;
    }

    // Finds the note that is sung at the given progress of the song and how far it is sung, from 0 to 1.
    private static (int Index, double Progress) FindSongNote(double trickProgress)
    {
        var beat = Math.Clamp(trickProgress, 0, 1) * SongBeats;
        for (var i = 0; i < Song.Length - 1; i++)
        {
            if (beat < Song[i].Beats)
            {
                return (i, beat / Song[i].Beats);
            }

            beat -= Song[i].Beats;
        }

        return (Song.Length - 1, Math.Min(1, beat / Song[^1].Beats));
    }

    /// <inheritdoc />
    protected override void Draw(DrawingContext context, AnimalRenderState state)
    {
        switch (state.State)
        {
            case AnimalAnimationState.Sleep:
                DrawSleeping(context, state.Phase);
                break;
            case AnimalAnimationState.Walk or AnimalAnimationState.Run:
                DrawWalking(context, state);
                break;
            default:
                DrawSitting(context, state);
                break;
        }
    }

    private static Face GetFace(AnimalRenderState state)
    {
        var face = state.State == AnimalAnimationState.Dragged
            ? Face.Startled
            : state.Trick switch
            {
                AnimalTrick.Speak => state.TrickProgress < 0.5 ? Face.Eat : Face.Happy,
                AnimalTrick.Wiggle or AnimalTrick.CapybaraSong or AnimalTrick.Happy => Face.Happy,
                AnimalTrick.Emote => Face.Cute,
                AnimalTrick.Shake => Face.Blink,
                AnimalTrick.LookAround => Face.LookAhead,
                _ => Face.Idle,
            };

        return state.IsBlinking && face is Face.Idle or Face.LookAhead ? Face.Blink : face;
    }

    private static void DrawSitting(DrawingContext context, AnimalRenderState state)
    {
        var face = GetFace(state);
        var isDragged = state.State == AnimalAnimationState.Dragged;
        var isPosing = face is Face.Happy or Face.Cute or Face.Eat;
        var breathe = Math.Sin(state.Phase * 0.5) * 4;

        if (!isDragged)
        {
            DrawShadow(context, ShadowBrush, state, DesignSize / 2, 29, 5);
        }

        using (context.PushTransform(ArtToDesign))
        {
            // The feet dangle and swing when the capybara is picked up.
            var dangle = isDragged
                ? Matrix.CreateTranslation(Math.Sin(state.Phase) * 8, 18)
                : Matrix.Identity;
            using (context.PushTransform(dangle))
            {
                DrawOutlined(context, FurDark, isPosing ? SplayedLeftFoot : LeftFoot);
                DrawOutlined(context, FurDark, isPosing ? SplayedRightFoot : RightFoot);
            }

            // The hind feet stay planted while the body twirls the booty.
            var (twirl, bob) = GetTwirl(state);
            using var bodyTransform = context.PushTransform(AroundTwirlPivot(Matrix.CreateRotation(
                twirl * TwirlAngle * Math.PI / 180)));
            DrawOutlined(context, Fur, Body);

            // The head drops a little when the capybara poses, the happy one also tilts it aside.
            var headShift = face switch
            {
                Face.Happy => new Vector(-4, PosingHeadDrop),
                Face.Cute or Face.Eat => new Vector(0, PosingHeadDrop),
                _ => default,
            };
            var headTransform = AroundTwirlPivot(Matrix.CreateRotation(-twirl * TwirlHeadTilt * Math.PI / 180))
                * Matrix.CreateTranslation(headShift.X, headShift.Y + breathe - (bob * TwirlHeadBob));
            using (context.PushTransform(headTransform))
            {
                DrawHead(context, face);
            }

            switch (face)
            {
                case Face.Cute:
                    // The paws are put together in front of the belly.
                    DrawOutlined(context, FurDark, CuteLeftPaw);
                    DrawOutlined(context, FurDark, CuteRightPaw);
                    break;
                case Face.Eat:
                    DrawWatermelon(context, state.TrickProgress);
                    break;
                default:
                    using (context.PushTransform(dangle))
                    {
                        DrawOutlined(context, FurDark, FrontLeftFoot);
                        DrawOutlined(context, FurDark, FrontRightFoot);
                    }

                    break;
            }
        }
    }

    // The booty swings out and back on every note of the song, to one side and then to the other,
    // the head bobs with it. The long notes are slow swings, the short ones are quick. A swing starts
    // and ends in the middle, so the dance flows from note to note without jerks.
    private static (double Twirl, double Bob) GetTwirl(AnimalRenderState state)
    {
        if (state.Trick != AnimalTrick.CapybaraSong || state.State == AnimalAnimationState.Dragged)
        {
            return (0, 0);
        }

        var (index, progress) = FindSongNote(state.TrickProgress);
        var swing = Math.Sin(Math.PI * progress) * Song[index].Swing;
        return (index % 2 == 0 ? swing : -swing, swing);
    }

    private static Matrix AroundTwirlPivot(Matrix transform) => Matrix.CreateTranslation(-TwirlPivotX, -TwirlPivotY)
        * transform
        * Matrix.CreateTranslation(TwirlPivotX, TwirlPivotY);

    private static void DrawHead(DrawingContext context, Face face)
    {
        DrawOutlined(context, Leaf, Sprout);
        DrawOutlined(context, Fur, Head);
        context.DrawEllipse(Cheek, null, new Point(257, 323), 39, 30);
        context.DrawEllipse(Cheek, null, new Point(551, 323), 39, 30);
        context.DrawGeometry(FurDark, null, NosePatch);

        // The nose and the mouth move up as the capybara lifts its chin, the watermelon hides the mouth.
        var isRaised = face is Face.Happy or Face.Cute or Face.Eat or Face.Startled;
        if (face != Face.Eat)
        {
            using (context.PushTransform(Matrix.CreateTranslation(0, isRaised ? -RaisedMouthShift : 0)))
            {
                context.DrawGeometry(null, FacePen, Mouth);
            }
        }

        using (context.PushTransform(Matrix.CreateTranslation(0, isRaised ? -RaisedNostrilsShift : 0)))
        {
            context.DrawGeometry(Outline, null, Nostrils);
        }

        context.DrawGeometry(FurDark, null, EarInners);
        context.DrawGeometry(Outline, null, EarRims);

        switch (face)
        {
            case Face.Blink:
                foreach (var center in EyeCenters)
                {
                    DrawClosedEye(context, center);
                }

                break;
            case Face.Happy:
                context.DrawGeometry(null, HappyEyePen, HappyEyes);
                break;
            case Face.Cute or Face.Startled:
                DrawCuteEyes(context, 1);
                break;
            case Face.Eat:
                DrawCuteEyes(context, -1);
                break;
            case Face.LookAhead:
                DrawEyes(context, EyeCenters, 1);
                break;
            default:
                DrawEyes(context, EyeCenters, 0);
                break;
        }
    }

    // The capybara holds a slice of watermelon in its paws and munches on it.
    private static void DrawWatermelon(DrawingContext context, double trickProgress)
    {
        var munch = Math.Abs(Math.Sin(Math.Clamp(trickProgress / 0.5, 0, 1) * Math.PI * 6)) * 10;
        using (context.PushTransform(Matrix.CreateTranslation(0, -munch)))
        {
            DrawOutlined(context, MelonRindBrush, MelonRind);
            context.DrawGeometry(MelonWhiteBrush, null, MelonWhite);
            context.DrawGeometry(MelonPulpBrush, null, MelonPulp);
            context.DrawGeometry(Outline, null, MelonSeeds);
            DrawOutlined(context, FurDark, EatLeftPaw);
            DrawOutlined(context, FurDark, EatRightPaw);
        }
    }

    private static void DrawClosedEye(DrawingContext context, Point center) =>
        context.DrawRectangle(Outline, null, new Rect(center.X - 33, center.Y - 12.145, 66, 24), 12, 12);

    // The highlight is on the side the capybara looks to: 1 is ahead, 0 is towards the nose.
    private static void DrawEyes(DrawingContext context, Point[] centers, int look)
    {
        for (var i = 0; i < centers.Length; i++)
        {
            var center = centers[i];
            var side = look > 0 || i == 0 ? 1 : -1;
            context.DrawEllipse(Outline, null, center, 23, 23);
            context.DrawEllipse(
                Brushes.White, null, new Point(center.X + (7.077 * side), center.Y - 8.846), 4.718, 4.718);
        }
    }

    // The small highlights are towards the middle of the face for 1 and towards the sides for -1.
    private static void DrawCuteEyes(DrawingContext context, int look)
    {
        for (var i = 0; i < CuteEyeCenters.Length; i++)
        {
            var center = CuteEyeCenters[i];
            var side = i == 0 ? look : -look;
            context.DrawEllipse(Outline, null, center, 34, 34);
            context.DrawEllipse(
                Brushes.White, null, new Point(center.X + (10.462 * side), center.Y - 13.077), 6.975, 6.975);
            context.DrawEllipse(
                Brushes.White, null, new Point(center.X - (12.565 * side), center.Y - 1.478), 13.304, 13.304);
        }
    }

    private static void DrawWalking(DrawingContext context, AnimalRenderState state)
    {
        var phase = state.Phase;
        var isRunning = state.State == AnimalAnimationState.Run;
        var gait = isRunning
            ? new Gait(RunReach, 34, RunStanceFraction)
            : new Gait(WalkReach, 20, WalkStanceFraction);

        // A trot: the body dips as each diagonal pair of legs reaches the middle of its stance.
        var midStance = 2 * (phase - (Math.PI * gait.StanceFraction));
        var bob = isRunning ? -(1 - Math.Cos(midStance)) * 10 : Math.Cos(midStance) * 6;

        DrawShadow(context, ShadowBrush, state, DesignSize / 2, 35, 5);
        using (context.PushTransform(Matrix.CreateTranslation(SideViewShiftX, 0) * ArtToDesign))
        {
            DrawLeg(context, FarHindLeg, phase + Math.PI, gait);
            DrawLeg(context, FarFrontLeg, phase, gait);

            using (context.PushTransform(Matrix.CreateTranslation(0, bob)))
            {
                DrawOutlined(context, Fur, SideBody);
            }

            DrawLeg(context, NearHindLeg, phase, gait);
            DrawLeg(context, NearFrontLeg, phase + Math.PI, gait);

            using (context.PushTransform(Matrix.CreateTranslation(0, bob * 0.7)))
            {
                if (isRunning)
                {
                    DrawRunningHead(context, state.IsBlinking);
                }
                else
                {
                    DrawSideHead(context, state.IsBlinking);
                }
            }
        }
    }

    private static void DrawLeg(DrawingContext context, Geometry leg, double phase, Gait gait)
    {
        var (offsetX, lift) = GetFootOffset(phase, gait);
        using (context.PushTransform(Matrix.CreateTranslation(offsetX, -lift)))
        {
            DrawOutlined(context, FurDark, leg);
        }
    }

    private static void DrawSideHead(DrawingContext context, bool isBlinking)
    {
        DrawOutlined(context, Leaf, SideSprout);
        context.DrawGeometry(FurDark, null, SideFarEarInner);
        context.DrawGeometry(Outline, null, SideFarEarRim);
        DrawOutlined(context, Fur, SideHead);
        context.DrawEllipse(Cheek, null, new Point(450, 368), 39, 30);
        context.DrawEllipse(Cheek, null, new Point(699, 368), 39, 30);
        if (isBlinking)
        {
            foreach (var center in SideEyeCenters)
            {
                DrawClosedEye(context, center);
            }
        }
        else
        {
            DrawEyes(context, SideEyeCenters, 0);
        }

        context.DrawGeometry(FurDark, null, SideNosePatch);
        context.DrawGeometry(null, FacePen, SideMouth);
        context.DrawGeometry(Outline, null, SideNostrils);
        context.DrawGeometry(FurDark, null, SideNearEarInner);
        context.DrawGeometry(Outline, null, SideNearEarRim);
    }

    // The running capybara stretches its head forward and down, the same as the sleeping one.
    private static void DrawRunningHead(DrawingContext context, bool isBlinking)
    {
        using (context.PushTransform(Matrix.CreateTranslation(RunningHeadShift.X, RunningHeadShift.Y)))
        {
            using (context.PushTransform(Matrix.CreateTranslation(RunningSproutShift.X, RunningSproutShift.Y)))
            {
                DrawOutlined(context, Leaf, SleepingSprout);
            }

            context.DrawGeometry(FurDark, null, SleepingFarEarInner);
            context.DrawGeometry(Outline, null, SleepingFarEarRim);
            DrawOutlined(context, Fur, SleepingHead);
            DrawRotated(context, new Point(441.661, 407.446), () =>
                context.DrawEllipse(Cheek, null, default, 39, 30));
            DrawRotated(context, new Point(682.176, 471.892), () =>
                context.DrawEllipse(Cheek, null, default, 39, 30));
            DrawRunningEye(context, RunningEyeCenters[1], isBlinking);
            context.DrawGeometry(FurDark, null, SleepingNosePatch);
            context.DrawGeometry(null, FacePen, SleepingMouth);
            context.DrawGeometry(Outline, null, SleepingNostrils);
            context.DrawGeometry(FurDark, null, SleepingNearEarInner);
            context.DrawGeometry(Outline, null, SleepingNearEarRim);
            DrawRunningEye(context, RunningEyeCenters[0], isBlinking);
        }
    }

    private static void DrawRunningEye(DrawingContext context, Point center, bool isBlinking)
    {
        if (isBlinking)
        {
            DrawRotated(context, center, () =>
                context.DrawRectangle(Outline, null, new Rect(-33, -12, 66, 24), 12, 12));
            return;
        }

        context.DrawEllipse(Outline, null, center, 23, 23);
        context.DrawEllipse(Brushes.White, null, new Point(center.X - 4.546, center.Y - 10.376), 4.718, 4.718);
    }

    private static void DrawSleeping(DrawingContext context, double phase)
    {
        var breathe = Math.Sin(phase);

        context.DrawEllipse(ShadowBrush, null, new Point(DesignSize / 2, GroundY + 2), 36, 5);
        using (context.PushTransform(Matrix.CreateTranslation(SleepingShiftX, 0) * ArtToDesign))
        {
            DrawOutlined(context, FurDark, SleepingFarPaw);

            // The body swells with the breath while staying on the ground.
            var breath = Matrix.CreateTranslation(0, -ArtGroundY)
                * Matrix.CreateScale(1, 1 + (breathe * 0.025))
                * Matrix.CreateTranslation(0, ArtGroundY);
            using (context.PushTransform(breath))
            {
                DrawOutlined(context, Fur, SleepingBody);
            }

            DrawOutlined(context, FurDark, SleepingNearPaw);
            DrawOutlined(context, FurDark, SleepingHindLeg);

            using (context.PushTransform(Matrix.CreateTranslation(0, breathe * 3)))
            {
                DrawOutlined(context, Leaf, SleepingSprout);
                context.DrawGeometry(FurDark, null, SleepingFarEarInner);
                context.DrawGeometry(Outline, null, SleepingFarEarRim);
                DrawOutlined(context, Fur, SleepingHead);
                DrawRotated(context, new Point(412.683, 399.682), () =>
                    context.DrawEllipse(Cheek, null, default, 39, 30));
                DrawRotated(context, new Point(682.176, 471.892), () =>
                    context.DrawEllipse(Cheek, null, default, 39, 30));
                DrawRotated(context, new Point(653.211, 392.723), () =>
                    context.DrawRectangle(Outline, null, new Rect(0, 0, 66, 24), 12, 12));
                context.DrawGeometry(FurDark, null, SleepingNosePatch);
                context.DrawGeometry(null, FacePen, SleepingMouth);
                context.DrawGeometry(Outline, null, SleepingNostrils);
                context.DrawGeometry(FurDark, null, SleepingNearEarInner);
                context.DrawGeometry(Outline, null, SleepingNearEarRim);
                DrawRotated(context, new Point(436.247, 335.723), () =>
                    context.DrawRectangle(Outline, null, new Rect(0, 0, 66, 24), 12, 12));
            }
        }

        DrawZzz(context, ZzzPen, new Point(8, 38), phase);
    }

    // The sleeping head is tilted, its round parts are rotated by 15 degrees around their anchor point.
    private static void DrawRotated(DrawingContext context, Point origin, Action draw)
    {
        var rotation = Matrix.CreateRotation(15 * Math.PI / 180) * Matrix.CreateTranslation(origin.X, origin.Y);
        using (context.PushTransform(rotation))
        {
            draw();
        }
    }

    private static void DrawOutlined(DrawingContext context, IBrush fill, Geometry geometry)
    {
        context.DrawGeometry(null, OutlinePen, geometry);
        context.DrawGeometry(fill, null, geometry);
    }

    private static Geometry Parse(string pathData) => StreamGeometry.Parse(pathData);

    /// <summary>
    /// A note of the capybara song.
    /// </summary>
    /// <param name="Beats">Length of the note in beats.</param>
    /// <param name="Text">Words sung so far in the line, shown in the speech bubble.</param>
    /// <param name="Swing">How far the booty swings on the note, from 0 to 1.</param>
    private readonly record struct SongNote(double Beats, string Text, double Swing);
}
