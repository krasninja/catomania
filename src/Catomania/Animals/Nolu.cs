using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// Nolu, a shaggy little forest creature with deer antlers, floppy ears and a light face patch. It stands
/// facing the viewer and follows the cursor with its eyes, turns sideways to walk on its short legs swinging
/// an arm and wagging the fluffy tail, hunches over looking for mushrooms when running, nibbles a fly agaric,
/// jumps for joy waving its arms and sleeps sitting with the arms folded.
/// </summary>
public sealed class Nolu : Animal
{
    // Nolu is drawn in the coordinates of its 609 x 721 front view artwork, the feet stand on the ground.
    // The artwork is scaled down, so the antlers stay in the design space at the top of a hop.
    private const double ArtScale = DesignSize / 1000;
    private const double ArtCenterX = 304.6;
    private const double ArtGroundY = 714;

    // The short legs take short steps, see Cat for how the stride is chosen. In the art coordinates.
    private const double WalkStanceFraction = 0.5;
    private const double RunStanceFraction = 0.35;
    private const double WalkReach = 28;
    private const double RunReach = 50;
    private const double WalkStride = 2 * WalkReach / WalkStanceFraction;
    private const double RunStride = 2 * RunReach / RunStanceFraction;

    // In degrees.
    private const double WalkArmSwing = 14;
    private const double RunArmSwing = 30;
    private const double TailWag = 8;
    private const double RunLean = 7;
    private const double ArmWave = 12;

    // The happy trick: a few joyful jumps, in the art coordinates and seconds.
    private const int HappyJumps = 3;
    private const double HappyJumpHeight = 45;
    private const double HappySeconds = 2.4;

    // How high the mushroom goes to the mouth on every nibble, in the art coordinates.
    private const double NibbleHeight = 12;
    private const int Nibbles = 5;
    private const double MushroomStemAngle = -15.7633;

    // How far the pupils move towards the cursor from the middle of the eyes, in the art coordinates.
    private const double PupilTravel = 20;
    private const double EyeRadius = 40.145;
    private const double PupilRadius = 8.59;
    private const double DotRadius = 5.433;

    // How much lower the sitting Nolu is than the standing one, the antlers go down with it.
    private const double SleepingDrop = 78.4;

    private static readonly Color FurColor = Color.FromUInt32(0xFF6D5353);
    private static readonly Color PupilColor = Color.FromUInt32(0xFF000000);

    private static readonly Matrix ArtToDesign = Matrix.CreateScale(ArtScale, ArtScale)
        * Matrix.CreateTranslation((DesignSize / 2) - (ArtCenterX * ArtScale), GroundY - (ArtGroundY * ArtScale));

    private static readonly IBrush Fur = new SolidColorBrush(FurColor);
    private static readonly IBrush Skin = new SolidColorBrush(Color.FromUInt32(0xFFB89E96));
    private static readonly IBrush Antler = new SolidColorBrush(Color.FromUInt32(0xFF473636));
    private static readonly IBrush EyeWhite = new SolidColorBrush(Color.FromUInt32(0xFFD9D9D9));
    private static readonly IBrush MushroomRed = new SolidColorBrush(Color.FromUInt32(0xFFA32222));
    private static readonly IBrush TongueBrush = new SolidColorBrush(Color.FromUInt32(0xFFAF4E4E));
    private static readonly IBrush Outline = new SolidColorBrush(PupilColor);
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));

    // Drawn over the fill like in the artwork, so half of the stroke covers the edge of the shape.
    private static readonly IPen OutlinePen = new Pen(Outline, 12, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen FacePatchPen = new Pen(Outline, 10, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen EyePen = new Pen(Outline, 7, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen MouthPen = new Pen(Outline, 5, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen TonguePen = new Pen(Outline, 4, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen PupilPen = new Pen(Outline, 2);
    private static readonly IPen SideEyePen = new Pen(Outline, 14);
    private static readonly IPen ZzzPen = new Pen(
        new SolidColorBrush(Color.FromUInt32(0xFFC9D6E3)), 1.8, null, PenLineCap.Round, PenLineJoin.Round);

    private static readonly Geometry LeftLeg = Parse(
        """
        M199.784 576.015C206.955 568.153 217.712 561.928 238.249 561.928C258.786 561.928 272.151
        567.17 281.279 580.275C289.721 592.397 292.036 610.743 292.036 628.435C292.036 646.126
        290.08 691.338 277.367 704.115C268.565 712.961 254.874 714.599 235.967 714.599C217.06
        714.599 196.198 707.392 196.198 694.287C196.198 681.182 204.999 679.216 206.629
        672.009C208.259 664.801 200.886 647.544 196.198 637.281C187.363 617.939 184.222 593.077
        199.784 576.015Z
        """);

    private static readonly Geometry RightLeg = Parse(
        """
        M409.733 575.95C402.562 568.124 391.804 561.928 371.267 561.928C350.731 561.928 337.366
        567.145 328.238 580.19C319.795 592.255 317.481 610.517 317.481 628.127C317.481 645.737
        319.437 690.739 332.15 703.457C340.951 712.262 354.642 713.892 373.549 713.892C392.456
        713.892 413.319 706.718 413.319 693.674C413.319 680.63 404.517 678.673 402.887
        671.499C401.258 664.325 408.631 647.147 413.319 636.932C422.154 617.68 425.294 592.933
        409.733 575.95Z
        """);

    private static readonly Geometry LeftHorn = Parse(
        """
        M246.661 101.33C255.106 106.852 249.259 124.068 229.445 129.914C209.632 135.761 208.852
        122.184 203.135 113.349C195.989 102.305 170.289 89.637 161.558 68.848C155.897 55.368 153.438
        39.29 158.96 23.698C163.203 11.717 173.697 1.053 183.321 8.432C193.066 15.903 176.5 59.387
        207.358 63.326C226.197 65.731 220.775 43.581 232.694 39.29C240.814 36.366 245.362 50.658
        242.114 61.377C238.865 72.097 228.471 77.294 229.445 83.79C230.42 90.287 238.758 96.163
        246.661 101.33Z
        """);

    private static readonly Geometry RightHorn = Parse(
        """
        M362.754 101.33C354.309 106.852 360.156 124.068 379.97 129.914C399.784 135.761 400.563
        122.184 406.28 113.349C413.426 102.305 439.126 89.637 447.857 68.848C453.519 55.368 455.978
        39.29 450.456 23.698C446.212 11.717 435.718 1.053 426.094 8.432C416.35 15.903 432.915 59.387
        402.057 63.326C383.218 65.731 388.641 43.581 376.721 39.29C368.601 36.366 364.053 50.658
        367.302 61.377C370.55 72.097 380.944 77.294 379.97 83.79C378.995 90.287 370.657 96.163
        362.754 101.33Z
        """);

    private static readonly Geometry LeftEar = Parse(
        """
        M135.917 204.979C153.962 209.998 159.821 215.603 163.302 226.535C170.351 248.671 163.728
        299.607 135.917 307.278C120.034 311.659 86.359 302.709 71.285 292.281C57.471 282.723 47.969
        266.646 49.275 261.949C50.706 256.804 53.258 259.614 59.512 254.158C64.996 249.375 80.899
        223.424 91.325 214.953C101.752 206.481 119.914 200.528 135.917 204.979Z
        """);

    private static readonly Geometry RightEar = Parse(
        """
        M472.69 204.979C454.644 209.998 448.786 215.603 445.304 226.535C438.255 248.671 444.878
        299.607 472.689 307.278C488.572 311.659 522.248 302.709 537.321 292.281C551.136 282.723
        560.638 266.646 559.331 261.949C557.9 256.804 555.349 259.614 549.094 254.158C543.61 249.375
        527.707 223.424 517.281 214.953C506.855 206.481 488.692 200.528 472.69 204.979Z
        """);

    private static readonly Geometry LeftArm = Parse(
        """
        M149.788 407.92C144.604 423.127 134.526 439.081 124.643 448.181C105.042 466.227 65.024
        480.728 37.516 471.769C24.357 467.483 13.885 458.396 12.427 446.967C10.968 435.537 19.501
        428.314 36.542 416.671C53.582 405.027 70.317 378.159 76.426 363.484C85.36 342.026 111.626
        326.636 133.308 340.193C155.348 353.975 157.49 385.331 149.788 407.92Z
        """);

    private static readonly Geometry RightArm = Parse(
        """
        M459.17 407.92C464.354 423.127 474.432 439.081 484.315 448.181C503.916 466.227 543.934
        480.728 571.442 471.769C584.6 467.483 595.073 458.396 596.531 446.967C597.99 435.537 589.456
        428.314 572.416 416.671C555.376 405.027 538.641 378.159 532.532 363.484C523.598 342.026
        497.332 326.636 475.65 340.193C453.609 353.975 451.468 385.331 459.17 407.92Z
        """);

    private static readonly Geometry Body = Parse(
        """
        M386.86 103.781C376.696 98.97 366.168 95.533 354.188 96.221C355.277 93.471 356.003 91.409
        357.455 82.13C345.838 84.879 319.337 88.316 312.803 91.065C314.981 86.941 316.796 83.161
        316.796 78.693C297.458 82.13 277.677 92.594 272.958 94.999C274.41 92.594 274.595 87.593
        274.958 84.499C265.156 85.874 247.821 93.471 237.657 98.282C217.243 107.945 197.676 118.061
        180.298 132.306C157.79 150.755 127.262 185.557 112.049 219.598C116.405 220.629 119.309
        220.629 121.851 219.598C114.59 233.344 103.291 276.037 94.624 285.238C99.343 287.988 104.425
        287.3 108.419 286.957C104.062 296.236 99.144 313.693 97.528 327.51C92.987 366.344 91.543
        396.586 92.262 414.801C93.171 437.827 100.069 485.253 106.241 512.06C108.782 511.716 110.597
        510.685 111.686 508.967C112.412 512.747 128.183 563.533 129.474 566.016C132.742 564.985
        133.105 564.641 134.557 562.923C143.481 571.425 154.254 597.902 167.955 607.256C170.133
        606.569 171.585 605.194 173.038 602.788C190.826 618.597 226.403 630.282 234.389
        631.313C235.841 630.282 236.931 627.876 237.657 626.158C242.376 628.22 261.984 632.687
        276.868 632.687C291.523 632.687 303.715 630.022 308.592 628.956L308.814 628.907C307.725
        630.625 305.547 633.375 305.547 635.78C326.24 634.062 367.625 626.845 371.251
        626.158C370.529 628.22 370.529 629.938 370.892 632C378.879 630.969 418.081 618.597 435.87
        602.788C437.322 605.194 438.774 606.569 440.952 607.256C454.653 597.902 469.999 571.171
        474.351 562.923C475.803 564.641 476.166 564.985 479.433 566.016C480.724 563.533 496.495
        512.747 497.221 508.967C498.31 510.685 500.126 511.716 502.667 512.06C508.838 485.253
        515.736 437.827 516.645 414.801C517.365 396.586 515.921 366.344 511.38 327.51C509.764
        313.693 504.845 296.236 500.489 286.957C504.482 287.3 509.564 287.988 514.284
        285.238C505.617 276.037 494.317 233.344 487.057 219.598C489.598 220.629 492.502 220.629
        496.858 219.598C481.645 185.557 451.117 150.755 428.609 132.306C411.231 118.061 407.274
        113.444 386.86 103.781Z
        """);

    private static readonly Geometry FacePatch = Parse(
        """
        M177.506 287.655C158.986 379.722 245.522 385.585 304.671 385.585C363.819 385.585 450.362
        379.722 431.843 287.655C417.82 217.944 379.84 170 304.671 170C224.038 170.227 192.415
        213.532 177.506 287.655Z
        """);

    private static readonly Geometry Nose = Parse(
        """
        M313.314 334.457C313.314 337.77 306.565 341.737 304.636 341.737C302.708 341.737 296.353
        337.77 295.958 334.457C295.958 330.437 299.843 328.034 304.636 328.034C309.429 328.034
        313.314 330.437 313.314 334.457Z
        """);

    private static readonly Geometry Mouth = Parse(
        """
        M311.574 361.454C311.574 365.237 308.302 366.402 304.266 366.402C300.23 366.402 296.958
        365.237 296.958 361.454C296.958 357.67 302.642 352.699 304.266 352.699C305.89 352.699
        311.574 357.67 311.574 361.454Z
        """);

    private static readonly Geometry ClosedEyes = Parse(
        """
        M281.958 309C273.423 320.459 258.917 328 242.458 328C225.999 328 211.493 320.459 202.958 309
        M405.958 309C397.423 320.459 382.917 328 366.458 328C349.999 328 335.493 320.459 326.958 309
        """);

    private static readonly Geometry CuteMouth = Parse(
        """
        M305.104 344.176C305.104 346.516 306.325 348.76 308.496 350.415C310.668 352.07 313.35
        353.023 316.685 352.999C319.947 352.977 324.104 350.415 324.104 350.415M305.104
        344.176C305.104 346.516 303.884 348.76 301.713 350.415C299.541 352.07 296.859 353.023
        293.524 352.999C290.262 352.977 286.104 350.415 286.104 350.415M305.104 344.176V338.588
        """);

    // The happy Nolu raises its arms and perks up its ears and face.
    private static readonly Geometry HappyLeftEar = Parse(
        """
        M153.285 219.956C167.634 231.996 170.646 239.523 169.289 250.916C166.541 273.984 139.424
        317.607 110.933 313.076C94.661 310.488 67.713 288.399 58.309 272.666C49.691 258.247 47.698
        239.678 50.832 235.943C54.264 231.853 55.424 235.467 63.376 233.09C70.348 231.006 95.568
        213.968 108.566 210.574C121.564 207.179 140.561 209.279 153.285 219.956Z
        """);

    private static readonly Geometry HappyRightEar = Parse(
        """
        M455.323 219.956C440.975 231.996 437.962 239.523 439.319 250.916C442.067 273.984 469.184
        317.607 497.676 313.076C513.947 310.488 540.895 288.399 550.299 272.666C558.917 258.247
        560.911 239.678 557.776 235.943C554.344 231.853 553.185 235.467 545.233 233.09C538.26
        231.006 513.041 213.968 500.043 210.574C487.045 207.179 468.047 209.279 455.323 219.956Z
        """);

    private static readonly Geometry HappyLeftArm = Parse(
        """
        M106.044 443.695C90.014 444.788 71.405 441.657 59.194 436.057C34.976 424.95 6.14 393.641
        3.776 364.808C2.645 351.015 6.981 337.846 16.959 332.085C26.937 326.324 36.896 331.404
        54.219 342.622C71.542 353.841 102.79 358.896 118.685 358.861C141.929 358.811 166.272 377.091
        162.144 402.327C157.948 427.981 129.854 442.071 106.044 443.695Z
        """);

    private static readonly Geometry HappyRightArm = Parse(
        """
        M504.343 443.695C520.372 444.788 538.981 441.657 551.192 436.057C575.41 424.95 604.246
        393.641 606.61 364.808C607.741 351.015 603.406 337.846 593.427 332.085C583.449 326.324
        573.49 331.404 556.167 342.622C538.844 353.841 507.596 358.896 491.701 358.861C468.458
        358.811 444.115 377.091 448.242 402.327C452.439 427.981 480.532 442.071 504.343 443.695Z
        """);

    private static readonly Geometry HappyFacePatch = Parse(
        """
        M177.36 275.655C158.841 367.722 245.377 373.585 304.525 373.585C363.674 373.585 450.216
        367.722 431.697 275.655C417.675 205.944 379.694 158 304.525 158C223.892 158.227 192.27
        201.532 177.36 275.655Z
        """);

    private static readonly Geometry HappyTongue = Parse(
        """
        M304.959 319C304.959 321.554 306.5 324.004 309.243 325.81C311.391 327.224 313.934 328.168
        316.959 328.5C316.959 328.5 313.169 339 304.959 339C296.748 339 292.959 328.5 292.959
        328.5C295.983 328.168 298.526 327.224 300.674 325.81C303.418 324.004 304.959 321.554 304.959
        319Z
        """);

    private static readonly Geometry HappyMouth = Parse(
        """
        M304.959 319.176C304.959 321.516 306.179 323.76 308.35 325.415C310.522 327.07 313.204
        328.023 316.539 327.999C319.801 327.977 323.959 325.415 323.959 325.415M304.959
        319.176C304.959 321.516 303.739 323.76 301.567 325.415C299.395 327.07 296.713 328.023
        293.379 327.999C290.116 327.977 285.959 325.415 285.959 325.415M304.959 319.176V313.588
        """);

    private static readonly Geometry HappyNose = Parse(
        """
        M313.315 310.457C313.315 313.77 306.565 317.737 304.637 317.737C302.708 317.737 296.353
        313.77 295.959 310.457C295.959 306.437 299.844 304.034 304.637 304.034C309.43 304.034
        313.315 306.437 313.315 310.457Z
        """);

    private static readonly Geometry HappyEyes = Parse(
        """
        M203.959 291C212.494 279.541 226.999 272 243.459 272C259.918 272 274.424 279.541 282.959 291
        M327.959 291C336.494 279.541 350.999 272 367.459 272C383.918 272 398.424 279.541 406.959 291
        """);

    // Sitting with the arms folded over the belly and the ears drooping.
    private static readonly Geometry SleepingLeftEar = Parse(
        """
        M122.228 278.187C140.959 278.187 148.105 282.017 154.388 291.617C167.111 311.054 174.379
        361.902 149.641 376.745C135.513 385.222 100.671 385.623 83.354 379.615C67.483 374.109 54.021
        361.166 54.021 356.29C54.021 350.95 57.232 352.973 61.796 346.041C65.798 339.963 74.165
        310.7 81.94 299.745C89.715 288.789 105.618 278.187 122.228 278.187Z
        """);

    private static readonly Geometry SleepingRightEar = Parse(
        """
        M486.379 278.187C467.648 278.187 460.502 282.017 454.219 291.617C441.496 311.054 434.228
        361.902 458.966 376.745C473.094 385.222 507.936 385.623 525.253 379.615C541.124 374.109
        554.586 361.166 554.586 356.29C554.586 350.95 551.375 352.973 546.811 346.041C542.809
        339.963 534.442 310.7 526.667 299.745C518.892 288.789 502.989 278.187 486.379 278.187Z
        """);

    private static readonly Geometry SleepingBody = Parse(
        """
        M354.188 174.62C366.168 173.933 376.696 177.37 386.861 182.181C407.275 191.844 411.231
        196.461 428.609 210.706C451.117 229.155 481.646 263.957 496.859 297.998C492.502 299.029
        489.598 299.029 487.057 297.998C494.317 311.744 505.617 354.437 514.284 363.638C509.565
        366.388 504.482 365.7 500.489 365.357C504.845 374.636 509.764 392.093 511.38 405.91C515.921
        444.744 517.365 474.986 516.645 493.201C515.736 516.227 508.839 563.653 502.667
        590.46C500.126 590.116 498.311 589.085 497.222 587.367C496.496 591.147 493.458 626.4 491.958
        635.9C486.458 632.9 484.41 626.118 482.958 624.4C480.458 640.4 454.653 676.302 440.952
        685.656C438.774 684.969 437.322 683.594 435.87 681.188C418.082 696.997 409.458 709.4 370.892
        710.4C339.693 711.21 264.426 709.745 233.225 709.745C218.341 709.745 218.445 704.807 213.725
        702.744C212.999 704.463 211.91 706.869 210.458 707.9C202.471 706.869 190.826 696.997 173.038
        681.188C171.586 683.594 170.134 684.969 167.956 685.656C154.254 676.302 125.958 642.9
        123.958 627.4C122.506 629.118 120.725 629.369 117.458 630.4C116.167 627.917 112.412 591.147
        111.686 587.367C110.597 589.085 108.782 590.116 106.241 590.46C100.069 563.653 93.172
        516.227 92.262 493.201C91.543 474.986 92.987 444.744 97.528 405.91C99.144 392.093 104.063
        374.636 108.419 365.357C104.426 365.7 99.343 366.388 94.624 363.638C103.291 354.437 114.59
        311.744 121.851 297.998C119.31 299.029 116.406 299.029 112.049 297.998C127.262 263.957
        157.791 229.155 180.298 210.706C197.676 196.461 217.243 186.345 237.657 176.683C247.822
        171.871 265.156 164.274 274.958 162.899C274.595 165.993 274.41 170.994 272.958
        173.399C277.677 170.994 297.458 160.53 316.797 157.093C316.797 161.561 314.981 165.341
        312.803 169.465C319.338 166.716 345.839 163.279 357.455 160.53C356.003 169.809 355.277
        171.871 354.188 174.62Z
        """);

    private static readonly Geometry SleepingLeftArm = Parse(
        """
        M124.885 544.15C134.351 557.132 148.711 569.375 160.85 575.129C184.926 586.542 227.445
        588.507 251.052 571.784C262.344 563.785 269.646 551.998 267.646 540.651C265.645 529.304
        255.352 524.941 235.623 518.882C215.894 512.823 191.936 492.134 181.745 479.936C166.843
        462.098 137.192 455.201 120.513 474.584C103.559 494.289 110.824 524.866 124.885 544.15Z
        """);

    private static readonly Geometry SleepingRightArm = Parse(
        """
        M484.636 538.576C475.169 551.559 460.809 563.802 448.67 569.556C424.594 580.969 382.076
        582.934 358.469 566.211C347.176 558.211 339.874 546.425 341.875 535.078C343.876 523.731
        354.168 519.367 373.897 513.308C393.626 507.25 417.584 486.561 427.775 474.363C442.677
        456.525 472.328 449.627 489.007 469.011C505.962 488.715 498.697 519.293 484.636 538.576Z
        """);

    private static readonly Geometry SleepingLeftFoot = Parse(
        """
        M153.787 605.611C144.834 611.315 136.864 620.833 133.298 641.057C129.732 661.282 132.549
        675.35 143.81 686.604C154.227 697.014 171.809 702.464 189.151 705.522C206.494 708.58 251.152
        714.469 265.884 704.157C276.084 697.018 280.067 683.818 283.35 665.199C286.633 646.579
        283.191 624.787 270.345 622.522C257.499 620.257 254.043 628.585 246.695 628.945C239.347
        629.304 223.711 619.06 214.465 612.669C197.039 600.625 173.213 593.235 153.787 605.611Z
        """);

    private static readonly Geometry SleepingRightFoot = Parse(
        """
        M454.336 605.622C463.325 611.32 471.323 620.833 474.889 641.057C478.456 661.282 475.614
        675.354 464.293 686.619C453.822 697.038 436.155 702.504 418.733 705.576C401.31 708.648
        356.445 714.573 341.655 704.271C331.415 697.14 327.424 683.941 324.141 665.321C320.858
        646.702 324.333 624.904 337.239 622.629C350.145 620.353 353.609 628.679 360.99
        629.033C368.371 629.387 384.086 619.129 393.379 612.73C410.893 600.67 434.832 593.26 454.336
        605.622Z
        """);

    private static readonly Geometry SleepingFacePatch = Parse(
        """
        M177.506 366.055C158.987 458.122 245.522 463.985 304.671 463.985C363.819 463.985 450.362
        458.122 431.843 366.055C417.821 296.344 379.84 248.4 304.671 248.4C224.038 248.627 192.415
        291.932 177.506 366.055Z
        """);

    private static readonly Geometry SleepingEyes = Parse(
        """
        M281.958 387.4C273.423 398.859 258.917 406.4 242.458 406.4C225.999 406.4 211.493 398.859
        202.958 387.4 M405.958 387.4C397.423 398.859 382.917 406.4 366.458 406.4C349.999 406.4
        335.493 398.859 326.958 387.4
        """);

    private static readonly Geometry SleepingNose = Parse(
        """
        M313.315 412.857C313.315 416.17 306.565 420.137 304.636 420.137C302.708 420.137 296.353
        416.17 295.958 412.857C295.958 408.837 299.844 406.434 304.636 406.434C309.429 406.434
        313.315 408.837 313.315 412.857Z
        """);

    private static readonly Geometry SleepingMouth = Parse(
        """
        M311.574 439.854C311.574 443.637 308.302 444.802 304.266 444.802C300.23 444.802 296.958
        443.637 296.958 439.854C296.958 436.07 302.642 431.099 304.266 431.099C305.89 431.099
        311.574 436.07 311.574 439.854Z
        """);

    // The side view, walking to the right.
    private static readonly Geometry HindLeg = Parse(
        """
        M287.057 575.688C279.886 567.825 269.128 561.6 248.592 561.6C228.055 561.6 214.69 566.842
        205.562 579.947C197.119 592.069 194.805 610.416 194.805 628.107C194.805 645.799 196.761
        691.01 209.474 703.788C218.275 712.633 231.967 714.272 250.873 714.272C269.78 714.272
        290.643 707.064 290.643 693.959C290.643 680.854 281.841 678.888 280.212 671.681C278.582
        664.473 285.955 647.216 290.643 636.953C299.478 617.611 302.618 592.749 287.057 575.688Z
        """);

    private static readonly Geometry ForeLeg = Parse(
        """
        M400.057 576.622C392.886 568.796 382.128 562.6 361.592 562.6C341.055 562.6 327.69 567.818
        318.562 580.862C310.119 592.928 307.805 611.19 307.805 628.799C307.805 646.409 309.761
        691.411 322.474 704.129C331.275 712.934 344.967 714.565 363.873 714.565C382.78 714.565
        403.643 707.39 403.643 694.346C403.643 681.302 394.841 679.345 393.212 672.171C391.582
        664.997 398.955 647.82 403.643 637.604C412.478 618.352 415.618 593.605 400.057 576.622Z
        """);

    private static readonly Geometry FarHorn = Parse(
        """
        M364.196 104.93C355.751 110.452 361.598 127.668 381.412 133.514C401.226 139.361 402.005
        125.784 407.722 116.949C414.868 105.905 440.568 93.237 449.299 72.448C454.96 58.968 457.419
        42.89 451.897 27.298C447.654 15.317 437.16 4.653 427.536 12.032C417.791 19.503 434.357
        62.987 403.499 66.926C384.66 69.331 390.083 47.181 378.163 42.89C370.043 39.966 365.495
        54.258 368.744 64.977C371.992 75.697 382.386 80.894 381.412 87.39C380.437 93.886 372.099
        99.763 364.196 104.93Z
        """);

    private static readonly Geometry NearHorn = Parse(
        """
        M348.249 104.93C356.694 110.452 350.847 127.668 331.033 133.514C311.219 139.361 310.44
        125.784 304.723 116.949C297.577 105.905 271.877 93.237 263.146 72.448C257.484 58.968 255.025
        42.89 260.547 27.298C264.791 15.317 275.285 4.653 284.909 12.032C294.653 19.503 278.088
        62.987 308.946 66.926C327.785 69.331 322.362 47.181 334.282 42.89C342.402 39.966 346.95
        54.258 343.701 64.977C340.453 75.697 330.059 80.894 331.033 87.39C332.008 93.886 340.346
        99.763 348.249 104.93Z
        """);

    private static readonly Geometry FarEar = Parse(
        """
        M313.327 213.36C328.887 217.687 331.254 230.6 332.254 240.1C334.591 262.297 333.254 300.6
        320.754 308.6C308.254 316.6 266.629 311.885 253.854 302.096C242.145 293.124 234.43 277.544
        235.736 272.847C237.167 267.702 239.293 270.393 244.942 264.769C249.895 259.838 264.687
        233.579 274.081 224.82C283.476 216.062 299.528 209.522 313.327 213.36Z
        """);

    private static readonly Geometry Tail = Parse(
        """
        M55.596 669.091C65.096 670.591 106.513 662.816 113.066 659.505C112.581 660.652 112.589
        661.959 113.095 663.916C141.638 659.505 169.361 639.645 182.595 607.591C206.331 550.1 147
        492.717 108.096 521.091C93 532.1 79.5 543.6 71 546.6C72.39 548.237 75 550.1 78.095
        551.591C61.595 556.591 50 557.1 33.095 555.091C33.5 558.1 36.769 561.943 37.595
        562.591C22.595 564.091 -2.905 560.091 -10.405 558.091C-11.405 560.591 -12.905 566.091
        -10.405 571.091C-17.905 568.091 -23.188 573.02 -23.905 578.591C-25 587.1 -21.405 593.1
        -15.405 601.091C-17 602.1 -19 602.6 -21.405 603.091C-17.905 615.591 -13.405 618.091 -6.107
        624.039C-6.595 623.753 -7.604 624.692 -8.586 625.511L-10.405 626.591C-5.905 634.591 2.095
        644.091 16.595 648.704C15.413 650.684 13.095 651.204 10.095 654.591C34.244 662.502 49.596
        663.091 57.734 663.916C56.53 665.71 56.316 666.465 55.596 669.091Z
        """);

    private static readonly Geometry SideBody = Parse(
        """
        M170 201.6C164 203.1 162.5 201.6 157.5 201.6C164 191.6 190 163.6 195.493 159.472C190.5
        159.472 188.5 162.472 184 159.472C195 151.472 231 130.728 237.5 128.6C232 127.1 228 129.6
        225 126.6C235 123.1 303.5 96.6 348.5 96.6C393.5 96.6 391.912 97.48 416.5 105.6C441.088
        113.72 458.591 117.502 484 146.6C479.591 148.002 472.5 153.1 470 154.1C476 160.1 480 166.6
        485.335 178.839C478 181.6 463.5 181.6 453 180.1C438.961 192.143 444.41 374.891 457.5
        379.6C483.91 389.1 494.719 420.384 494 438.6C490.341 435.717 488.349 436.83 487.396
        440.6C484.053 453.832 493.521 499.798 488.718 520.66C486.176 520.316 484.361 519.285 483.272
        517.567C482.546 521.347 466.775 572.133 465.484 574.616C462.217 573.585 461.854 573.241
        460.402 571.523C451.477 580.025 440.704 606.502 427.003 615.856C424.825 615.169 423.373
        613.795 421.921 611.389C404.132 627.198 368.556 638.882 360.569 639.913C359.117 638.882
        358.028 636.477 357.302 634.758C352.582 636.82 332.974 641.288 318.09 641.288C303.435
        641.288 291.243 638.622 286.367 637.556L286.144 637.508C287.233 639.226 289.411 641.975
        289.411 644.381C268.719 642.663 227.333 635.446 223.708 634.758C224.429 636.82 224.429
        638.539 224.066 640.601C216.079 639.57 176.877 627.198 159.089 611.389C157.636 613.795
        156.184 615.169 154.006 615.856C140.305 606.502 124.959 579.771 120.608 571.523C119.156
        573.241 118.793 573.585 115.525 574.616C114.234 572.133 104.463 506.347 103.737
        502.567C102.648 504.285 100.833 505.316 98.291 505.66C92.12 478.854 100 441.1 102.674
        424.1C98.291 426.6 99.5 426.1 94 427.6C102 365.1 111.5 351.1 116.469 341.557C112.5 344.1
        110.826 344.557 105 346.1C108.763 330.6 122.641 287.945 129.901 274.198C127.36 275.229
        124.456 275.229 120.1 274.198C125.479 262.161 152 227.643 170 201.6Z
        """);

    private static readonly Geometry SideArm = Parse(
        """
        M304.212 442.781C309.396 457.989 319.474 473.943 329.357 483.042C348.958 501.089 388.976
        515.59 416.484 506.63C429.643 502.345 440.115 493.257 441.573 481.828C443.032 470.399
        434.499 463.176 417.458 451.533C400.418 439.889 383.683 413.02 377.574 398.346C368.64
        376.888 342.374 361.498 320.692 375.055C298.652 388.837 296.51 420.192 304.212 442.781Z
        """);

    private static readonly Geometry SideFacePatch = Parse(
        """
        M397.86 230.072C379.918 267.212 378.961 314.546 384.5 345.1C390.039 375.654 407 396.6 439.5
        396.6C467.541 396.6 496 372.6 505.5 349.1C508.43 341.852 498.204 342.508 490.727
        328.428C484.54 316.776 492.187 278.305 489.712 248.448C483.817 197.656 465.296 183.465
        450.448 178.368C440.233 174.861 415.802 192.932 397.86 230.072Z
        """);

    private static readonly Geometry SideEye = Parse(
        """
        M415 316.432C415 340.987 427.327 360.893 445 360.893C462.673 360.893 477 340.987 477
        316.432C477 291.877 462.673 271.971 445 271.971C427.327 271.971 415 291.877 415 316.432Z
        """);

    private static readonly Geometry SideNose = Parse(
        """
        M501 343.163C501 346.547 506.056 350.6 507.5 350.6C508.944 350.6 513.705 346.547 514
        343.163C514 339.055 511.09 336.6 507.5 336.6C503.91 336.6 501 339.055 501 343.163Z
        """);

    // The running Nolu hunches over, looking for mushrooms.
    private static readonly Geometry HuntingBody = Parse(
        """
        M169.966 201.6C163.966 203.1 162.466 201.6 157.466 201.6C163.966 191.6 189.966 163.6 195.46
        159.472C190.466 159.472 188.466 162.472 183.966 159.472C194.966 151.472 230.966 130.728
        237.466 128.6C231.966 127.1 227.966 129.6 224.966 126.6C234.966 123.1 303.467 96.6 348.466
        96.6C393.466 96.6 391.878 97.48 416.466 105.6C441.055 113.72 458.557 117.502 483.967
        146.6C479.557 148.002 472.467 153.1 469.967 154.1C475.967 160.1 479.967 166.6 485.301
        178.839C477.966 181.6 463.467 181.6 452.967 180.1C438.927 192.143 444.377 374.891 457.467
        379.6C483.876 389.1 494.686 420.384 493.966 438.6C490.467 438.6 488.07 437.6 485.967
        437.6C488.467 450.1 493.487 499.798 488.684 520.66C486.143 520.316 484.328 519.285 483.239
        517.567C482.513 521.347 466.741 572.133 465.45 574.616C462.183 573.585 461.82 573.241
        460.368 571.523C451.443 580.025 440.67 606.502 426.969 615.856C424.791 615.169 423.339
        613.795 421.887 611.389C404.099 627.198 368.522 638.882 360.535 639.913C359.083 638.882
        357.994 636.477 357.268 634.758C352.549 636.82 332.941 641.288 318.057 641.288C303.401
        641.288 291.209 638.622 286.333 637.556L286.11 637.508C287.199 639.226 289.377 641.975
        289.377 644.381C268.685 642.663 227.3 635.446 223.674 634.758C224.395 636.82 224.395 638.539
        224.032 640.601C216.046 639.57 176.843 627.198 159.055 611.389C157.603 613.795 156.151
        615.169 153.973 615.856C140.271 606.502 124.926 579.771 120.574 571.523C119.122 573.241
        118.759 573.585 115.492 574.616C114.201 572.133 104.429 506.347 103.703 502.567C102.614
        504.285 100.799 505.316 98.258 505.66C92.086 478.854 99.966 441.1 102.641 424.1C98.258 426.6
        99.466 426.1 93.966 427.6C101.966 365.1 111.466 351.1 116.436 341.557C112.466 344.1 110.792
        344.557 104.966 346.1C108.729 330.6 122.607 287.945 129.868 274.198C127.326 275.229 124.422
        275.229 120.066 274.198C125.445 262.161 151.966 227.643 169.966 201.6Z
        """);

    // Nolu holds a fly agaric in both arms and nibbles at it.
    private static readonly Geometry MushroomCap = Parse(
        """
        M396.399 409.211C410.92 429.72 368.271 458.714 320.169 472.292C272.067 485.871 218.767
        483.275 222.206 458.382C225.791 432.432 256.02 364.672 287.298 355.843C318.575 347.014
        377.391 382.365 396.399 409.211Z
        """);

    private static readonly Geometry EatingLeftArm = Parse(
        """
        M126.885 440.75C136.351 453.732 150.711 465.975 162.85 471.729C186.926 483.142 229.445
        485.107 253.052 468.384C264.344 460.385 271.646 448.598 269.646 437.251C267.645 425.904
        257.352 421.541 237.623 415.482C217.894 409.423 193.936 388.734 183.745 376.536C168.843
        358.698 139.192 351.801 122.513 371.184C105.558 390.889 112.824 421.466 126.885 440.75Z
        """);

    private static readonly Geometry EatingRightArm = Parse(
        """
        M486.635 435.176C477.169 448.159 462.809 460.402 450.67 466.156C426.594 477.569 384.075
        479.534 360.468 462.811C349.176 454.811 341.874 443.025 343.874 431.678C345.875 420.331
        356.168 415.967 375.897 409.908C395.626 403.85 419.584 383.161 429.775 370.963C444.677
        353.125 474.328 346.227 491.007 365.611C507.962 385.315 500.696 415.893 486.635 435.176Z
        """);

    private static readonly Geometry ClosedSideEye = Parse("M418 320C427 335 463 335 472 320");

    private static readonly Point[] EyeCenters = [new(240.509, 309.982), new(367.27, 309.982)];
    private static readonly Point[] IdlePupils = [new(242.91, 325.687), new(365.875, 325.687)];
    private static readonly Point[] AheadPupils = [new(258.91, 321.687), new(381.875, 321.687)];
    private static readonly Point[] CutePupils = [new(229.548, 292.687), new(352.513, 292.687)];
    private static readonly Point[] EatingPupils = [new(254.548, 330.59), new(352.548, 330.59)];
    private static readonly Point[] MushroomSpots = [new(299.72, 401.692), new(341.701, 417.897), new(276.259, 436.37)];
    private static readonly Point MushroomStemCorner = new(284.603, 447.522);
    private static readonly Point[] Dots = [new(270.118, 238.632), new(339.191, 238.632)];
    private static readonly Point[] HappyDots = [new(263.892, 219.934), new(345.892, 219.934)];
    private static readonly Point[] SleepingDots = [new(270.118, 317.032), new(339.191, 317.032)];

    // The raised arms wave around the shoulders.
    private static readonly Point[] Shoulders = [new(160, 400), new(449, 400)];

    private static readonly Point SideShoulder = new(319, 383.6);
    private static readonly Point TailBase = new(114, 603.6);
    private static readonly Point SidePupil = new(461.41, 331.834);
    private static readonly Point HuntingPupil = new(457.41, 342.358);
    private static readonly Point SideDot = new(460.066, 247.643);
    private static readonly Point Feet = new(ArtCenterX, ArtGroundY);

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
    public override AnimalKind Kind => AnimalKind.Nolu;

    /// <inheritdoc />
    public override Color BodyColor => FurColor;

    /// <inheritdoc />
    public override Color EyeColor => PupilColor;

    /// <inheritdoc />
    public override string Sound => "Nolu-nolu!";

    /// <inheritdoc />
    public override AnimalSimulatorOptions CreateSimulatorOptions() => new()
    {
        WalkStrideLength = WalkStride * ArtScale / DesignSize,
        RunStrideLength = RunStride * ArtScale / DesignSize,
        Tricks = [.. new AnimalSimulatorOptions().Tricks, AnimalTrick.Happy],
        TrickDurations = new Dictionary<AnimalTrick, double>
        {
            [AnimalTrick.Happy] = HappySeconds,
        },
    };

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
                DrawStanding(context, state);
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
                AnimalTrick.Wiggle or AnimalTrick.Happy => Face.Happy,
                AnimalTrick.Emote => Face.Cute,
                AnimalTrick.Shake => Face.Blink,
                AnimalTrick.LookAround => Face.LookAhead,
                _ => Face.Idle,
            };

        return state.IsBlinking && face is Face.Idle or Face.LookAhead or Face.Cute ? Face.Blink : face;
    }

    private static void DrawStanding(DrawingContext context, AnimalRenderState state)
    {
        var face = GetFace(state);
        var isDragged = state.State == AnimalAnimationState.Dragged;
        var isHappy = face == Face.Happy;
        var breathe = isDragged ? 0 : Math.Sin(state.Phase * 0.5) * 5;

        if (!isDragged)
        {
            DrawShadow(context, ShadowBrush, state, DesignSize / 2, 23, 5);
        }

        using (context.PushTransform(Matrix.CreateTranslation(0, -GetHappyJump(state)) * ArtToDesign))
        {
            // The legs dangle and swing when Nolu is picked up.
            var dangle = isDragged
                ? Matrix.CreateTranslation(Math.Sin(state.Phase) * 8, 18)
                : Matrix.Identity;
            using (context.PushTransform(dangle))
            {
                DrawOutlined(context, Skin, LeftLeg);
                DrawOutlined(context, Skin, RightLeg);
            }

            using (context.PushTransform(Matrix.CreateTranslation(0, breathe)))
            {
                DrawOutlined(context, Antler, LeftHorn);
                DrawOutlined(context, Antler, RightHorn);
                DrawOutlined(context, Skin, isHappy ? HappyLeftEar : LeftEar);
                DrawOutlined(context, Skin, isHappy ? HappyRightEar : RightEar);

                // The arms go up in joy and in fright, the happy Nolu waves them. The eating Nolu holds
                // the mushroom in front of the body, those arms are drawn over it later.
                if (isHappy || isDragged)
                {
                    var wave = isHappy ? GetArmWave(state) : 0;
                    DrawRotated(context, Shoulders[0], wave, () => DrawOutlined(context, Skin, HappyLeftArm));
                    DrawRotated(context, Shoulders[1], -wave, () => DrawOutlined(context, Skin, HappyRightArm));
                }
                else if (face != Face.Eat)
                {
                    DrawOutlined(context, Skin, LeftArm);
                    DrawOutlined(context, Skin, RightArm);
                }

                DrawOutlined(context, Fur, Body);
                if (isHappy)
                {
                    DrawHappyFace(context);
                }
                else
                {
                    DrawFace(context, face, state);
                }

                if (face == Face.Eat)
                {
                    DrawMushroom(context, state.TrickProgress);
                }
            }
        }
    }

    // Nolu jumps up and lands a few times during the happy trick, the jumps are quick parabolas.
    private static double GetHappyJump(AnimalRenderState state)
    {
        if (state.Trick != AnimalTrick.Happy || state.State == AnimalAnimationState.Dragged)
        {
            return 0;
        }

        var jumps = Math.Clamp(state.TrickProgress, 0, 1) * HappyJumps;
        return Math.Sin(Math.PI * (jumps - Math.Floor(jumps))) * HappyJumpHeight;
    }

    // Nolu brings the mushroom up to the mouth and nibbles at it during the first half of the trick.
    private static void DrawMushroom(DrawingContext context, double trickProgress)
    {
        var nibble = Math.Abs(Math.Sin(Math.Clamp(trickProgress / 0.5, 0, 1) * Math.PI * Nibbles)) * NibbleHeight;
        using (context.PushTransform(Matrix.CreateTranslation(0, -nibble)))
        {
            var stem = Matrix.CreateRotation(MushroomStemAngle * Math.PI / 180)
                * Matrix.CreateTranslation(MushroomStemCorner.X, MushroomStemCorner.Y);
            using (context.PushTransform(stem))
            {
                context.DrawRectangle(EyeWhite, OutlinePen, new Rect(0, 0, 55, 103), 27.5, 27.5);
            }

            DrawOutlined(context, MushroomRed, MushroomCap);
            foreach (var spot in MushroomSpots)
            {
                context.DrawEllipse(EyeWhite, null, spot, 13, 13);
            }

            DrawOutlined(context, Skin, EatingRightArm);
            DrawOutlined(context, Skin, EatingLeftArm);
        }
    }

    // The arms wave a few times during the trick, rising and fading out smoothly.
    private static double GetArmWave(AnimalRenderState state)
    {
        var progress = Math.Clamp(state.TrickProgress, 0, 1);
        return Math.Sin(progress * TwoPi * 4) * Math.Sin(Math.PI * progress) * ArmWave;
    }

    private static void DrawFace(DrawingContext context, Face face, AnimalRenderState state)
    {
        DrawOutlined(context, Skin, FacePatch, FacePatchPen);
        if (face == Face.Blink)
        {
            context.DrawGeometry(null, EyePen, ClosedEyes);
        }
        else
        {
            for (var i = 0; i < EyeCenters.Length; i++)
            {
                context.DrawEllipse(EyeWhite, EyePen, EyeCenters[i], EyeRadius, EyeRadius);
                context.DrawEllipse(Outline, PupilPen, GetPupilCenter(face, i, state), PupilRadius, PupilRadius);
            }
        }

        context.DrawGeometry(Outline, null, Nose);
        DrawDots(context, Dots);
        switch (face)
        {
            case Face.Cute:
                context.DrawGeometry(null, MouthPen, CuteMouth);
                break;
            case Face.Startled:
                context.DrawEllipse(Outline, null, new Point(ArtCenterX, 362), 9, 11);
                break;
            default:
                context.DrawGeometry(Outline, null, Mouth);
                break;
        }
    }

    private static Point GetPupilCenter(Face face, int eyeIndex, AnimalRenderState state)
    {
        switch (face)
        {
            case Face.Cute or Face.Startled:
                return CutePupils[eyeIndex];
            case Face.Eat:
                return EatingPupils[eyeIndex];
            case Face.LookAhead:
                return AheadPupils[eyeIndex];
        }

        var direction = state.CursorDirection;
        if (direction.Length < double.Epsilon)
        {
            return IdlePupils[eyeIndex];
        }

        // AnimalView mirrors Nolu when it faces left, so compensate to keep the pupils on the cursor side.
        var x = direction.X * (state.FacingRight ? 1 : -1);
        var length = Math.Sqrt((x * x) + (direction.Y * direction.Y));
        var center = EyeCenters[eyeIndex];
        return new Point(
            center.X + (x / length * PupilTravel),
            center.Y + (direction.Y / length * PupilTravel));
    }

    // The happy Nolu lifts its face, squints and shows the tongue.
    private static void DrawHappyFace(DrawingContext context)
    {
        DrawOutlined(context, Skin, HappyFacePatch, FacePatchPen);
        DrawOutlined(context, TongueBrush, HappyTongue, TonguePen);
        context.DrawGeometry(null, MouthPen, HappyMouth);
        context.DrawGeometry(Outline, null, HappyNose);
        context.DrawGeometry(null, EyePen, HappyEyes);
        DrawDots(context, HappyDots);
    }

    private static void DrawDots(DrawingContext context, Point[] dots)
    {
        foreach (var dot in dots)
        {
            context.DrawEllipse(Outline, null, dot, DotRadius, DotRadius);
        }
    }

    private static void DrawWalking(DrawingContext context, AnimalRenderState state)
    {
        var phase = state.Phase;
        var isRunning = state.State == AnimalAnimationState.Run;
        var gait = isRunning
            ? new Gait(RunReach, 40, RunStanceFraction)
            : new Gait(WalkReach, 22, WalkStanceFraction);

        // The body dips as each leg reaches the middle of its stance.
        var midStance = 2 * (phase - (Math.PI * gait.StanceFraction));
        var bob = isRunning ? -(1 - Math.Cos(midStance)) * 10 : Math.Cos(midStance) * 6;
        var armSwing = Math.Sin(phase) * (isRunning ? RunArmSwing : WalkArmSwing);
        var lean = isRunning ? RunLean : 0;

        DrawShadow(context, ShadowBrush, state, DesignSize / 2, 24, 5);
        using (context.PushTransform(AroundPoint(Feet, Matrix.CreateRotation(lean * Math.PI / 180)) * ArtToDesign))
        {
            DrawLeg(context, HindLeg, phase + Math.PI, gait);
            DrawLeg(context, ForeLeg, phase, gait);

            using (context.PushTransform(Matrix.CreateTranslation(0, bob)))
            {
                DrawOutlined(context, Antler, FarHorn);
                DrawOutlined(context, Skin, FarEar);
                DrawRotated(context, TailBase, Math.Sin(phase * 2) * TailWag, () => DrawOutlined(context, Fur, Tail));
                DrawOutlined(context, Fur, isRunning ? HuntingBody : SideBody);
                DrawOutlined(context, Antler, NearHorn);
                DrawRotated(context, SideShoulder, -armSwing, () => DrawOutlined(context, Skin, SideArm));
                DrawOutlined(context, Skin, SideFacePatch, FacePatchPen);
                if (state.IsBlinking)
                {
                    context.DrawGeometry(null, EyePen, ClosedSideEye);
                }
                else
                {
                    // The outline of the eye is drawn inside it, like in the artwork.
                    context.DrawGeometry(EyeWhite, null, SideEye);
                    using (context.PushGeometryClip(SideEye))
                    {
                        context.DrawGeometry(null, SideEyePen, SideEye);
                    }

                    // The running Nolu looks down at the ground ahead, hunting for mushrooms.
                    context.DrawEllipse(Outline, null, isRunning ? HuntingPupil : SidePupil, 7.59, 7.59);
                }

                context.DrawGeometry(Outline, null, SideNose);
                context.DrawEllipse(Outline, null, SideDot, 4.93, 4.93);
            }
        }
    }

    private static void DrawLeg(DrawingContext context, Geometry leg, double phase, Gait gait)
    {
        var (offsetX, lift) = GetFootOffset(phase, gait);
        using (context.PushTransform(Matrix.CreateTranslation(offsetX, -lift)))
        {
            DrawOutlined(context, Skin, leg);
        }
    }

    private static void DrawSleeping(DrawingContext context, double phase)
    {
        var breathe = Math.Sin(phase) * 4;

        context.DrawEllipse(ShadowBrush, null, new Point(DesignSize / 2, GroundY + 2), 24, 5);
        using (context.PushTransform(ArtToDesign))
        {
            using (context.PushTransform(Matrix.CreateTranslation(0, breathe)))
            {
                using (context.PushTransform(Matrix.CreateTranslation(0, SleepingDrop)))
                {
                    DrawOutlined(context, Antler, LeftHorn);
                    DrawOutlined(context, Antler, RightHorn);
                }

                DrawOutlined(context, Skin, SleepingLeftEar);
                DrawOutlined(context, Skin, SleepingRightEar);
                DrawOutlined(context, Fur, SleepingBody);
                DrawOutlined(context, Skin, SleepingRightArm);
                DrawOutlined(context, Skin, SleepingLeftArm);
            }

            DrawOutlined(context, Skin, SleepingRightFoot);
            DrawOutlined(context, Skin, SleepingLeftFoot);

            using (context.PushTransform(Matrix.CreateTranslation(0, breathe)))
            {
                DrawOutlined(context, Skin, SleepingFacePatch, FacePatchPen);
                context.DrawGeometry(null, EyePen, SleepingEyes);
                context.DrawGeometry(Outline, null, SleepingNose);
                DrawDots(context, SleepingDots);
                context.DrawGeometry(Outline, null, SleepingMouth);
            }
        }

        DrawZzz(context, ZzzPen, new Point(64, 40), phase);
    }

    private static void DrawRotated(DrawingContext context, Point pivot, double angle, Action draw)
    {
        using (context.PushTransform(AroundPoint(pivot, Matrix.CreateRotation(angle * Math.PI / 180))))
        {
            draw();
        }
    }

    private static Matrix AroundPoint(Point point, Matrix transform) => Matrix.CreateTranslation(-point.X, -point.Y)
        * transform
        * Matrix.CreateTranslation(point.X, point.Y);

    private static void DrawOutlined(DrawingContext context, IBrush fill, Geometry geometry) =>
        DrawOutlined(context, fill, geometry, OutlinePen);

    private static void DrawOutlined(DrawingContext context, IBrush fill, Geometry geometry, IPen pen) =>
        context.DrawGeometry(fill, pen, geometry);

    private static Geometry Parse(string pathData) => StreamGeometry.Parse(pathData);
}
