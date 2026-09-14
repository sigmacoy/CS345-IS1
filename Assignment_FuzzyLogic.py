def triangular_membership(x, a, b, c):
    """
    Triangular Membership Function
    a = left foot
    b = peak
    c = right foot
    """

    if x < a or x > c:
        return 0.0

    if x == b:
        return 1.0

    if x < b:
        return (x - a) / (b - a)

    return (c - x) / (c - b)


def draw_bar(label, value, max_val, unit=""):
    bar_length = 20
    filled = int((value / max_val) * bar_length)
    filled = min(max(filled, 0), bar_length)

    bar = "=" * filled + " " * (bar_length - filled)

    print(f"{label:<12}: [{bar}] {value:.2f}{unit}")


def main():

    print("=== BMX Landing Impact Calculator ===")

    try:
        height = float(input("Enter Drop Height (0 to 5m): "))
        height = min(max(height, 0.0), 5.0)

        speed = float(input("Enter Landing Speed (0 to 40km/h): "))
        speed = min(max(speed, 0.0), 40.0)

    except ValueError:
        print("Invalid input! Please enter numeric values.")
        return

    print("\nSelect Logic:")
    print("a - Mamdani")
    print("b - Sugeno")

    logic = input("Choice (a/b): ").strip().lower()

    if logic not in ["a", "b"]:
        print("Invalid choice.")
        return

    # -------------------------
    # INPUT VISUALIZATION
    # -------------------------

    print("\n--- Inputs ---")

    draw_bar("Height", height, 5.0, "m")
    draw_bar("Speed", speed, 40.0, "km/h")

    # -------------------------
    # FUZZIFICATION
    # -------------------------

    h_low = triangular_membership(height, -1, 0, 2.5)
    h_med = triangular_membership(height, 0, 2.5, 5)
    h_high = triangular_membership(height, 2.5, 5, 6)

    s_slow = triangular_membership(speed, -1, 0, 20)
    s_med = triangular_membership(speed, 0, 20, 40)
    s_fast = triangular_membership(speed, 20, 40, 41)

    print("\n--- Fuzzification Results ---")

    print(
        f"Height [Low: {h_low:.2f}, "
        f"Medium: {h_med:.2f}, "
        f"High: {h_high:.2f}]"
    )

    print(
        f"Speed  [Slow: {s_slow:.2f}, "
        f"Medium: {s_med:.2f}, "
        f"Fast: {s_fast:.2f}]"
    )

    # -------------------------
    # RULE EVALUATION
    # -------------------------

    # Rule 1:
    # IF Height is Low AND Speed is Slow
    # THEN Bend is Stiff
    r1 = min(h_low, s_slow)

    # Rule 2:
    # IF Height is Low AND Speed is Medium
    # THEN Bend is Moderate
    r2 = min(h_low, s_med)

    # Rule 3:
    # IF Height is Low AND Speed is Fast
    # THEN Bend is Moderate
    r3 = min(h_low, s_fast)

    # Rule 4:
    # IF Height is Medium AND Speed is Slow
    # THEN Bend is Moderate
    r4 = min(h_med, s_slow)

    # Rule 5:
    # IF Height is Medium AND Speed is Medium
    # THEN Bend is Moderate
    r5 = min(h_med, s_med)

    # Rule 6:
    # IF Height is Medium AND Speed is Fast
    # THEN Bend is Deep
    r6 = min(h_med, s_fast)

    # Rule 7:
    # IF Height is High AND Speed is Slow
    # THEN Bend is Moderate
    r7 = min(h_high, s_slow)

    # Rule 8:
    # IF Height is High AND Speed is Medium
    # THEN Bend is Deep
    r8 = min(h_high, s_med)

    # Rule 9:
    # IF Height is High AND Speed is Fast
    # THEN Bend is Deep
    r9 = min(h_high, s_fast)

    print("\n--- Rule Firing Strengths ---")

    print(f"Rule 1  (Stiff):    {r1:.2f}")
    print(f"Rule 2  (Moderate): {r2:.2f}")
    print(f"Rule 3  (Moderate): {r3:.2f}")
    print(f"Rule 4  (Moderate): {r4:.2f}")
    print(f"Rule 5  (Moderate): {r5:.2f}")
    print(f"Rule 6  (Deep):     {r6:.2f}")
    print(f"Rule 7  (Moderate): {r7:.2f}")
    print(f"Rule 8  (Deep):     {r8:.2f}")
    print(f"Rule 9  (Deep):     {r9:.2f}")

    # -------------------------
    # MAMDANI
    # -------------------------

    if logic == "a":

        print("\n=== Mamdani Output ===")

        numerator = 0.0
        denominator = 0.0

        step = 0.5
        y = 0.0

        while y <= 100.0:

            # Output membership functions

            out_stiff = triangular_membership(
                y, -1, 0, 50
            )

            out_moderate = triangular_membership(
                y, 25, 50, 75
            )

            out_deep = triangular_membership(
                y, 50, 100, 101
            )

            # Clip output memberships
            clipped_stiff = min(r1, out_stiff)

            clipped_moderate = max(
                min(r2, out_moderate),
                min(r3, out_moderate),
                min(r4, out_moderate),
                min(r5, out_moderate),
                min(r7, out_moderate)
            )

            clipped_deep = max(
                min(r6, out_deep),
                min(r8, out_deep),
                min(r9, out_deep)
            )

            # Aggregate all outputs
            aggregated_y = max(
                clipped_stiff,
                clipped_moderate,
                clipped_deep
            )

            numerator += y * aggregated_y * step
            denominator += aggregated_y * step

            y += step

        crisp_output = 0.0

        if denominator > 0:
            crisp_output = numerator / denominator

        draw_bar(
            "Knee Bend",
            crisp_output,
            100.0,
            "%"
        )

    # -------------------------
    # SUGENO
    # -------------------------

    elif logic == "b":

        print("\n=== Sugeno Output ===")

        # Each rule produces a crisp mathematical value.

        z_stiff = (height * 5) + (speed * 0.5)

        z_moderate = (height * 10) + (speed * 0.5)

        z_deep = (height * 15) + (speed * 0.5)

        numerator = (
            r1 * z_stiff
            + r2 * z_moderate
            + r3 * z_moderate
            + r4 * z_moderate
            + r5 * z_moderate
            + r6 * z_deep
            + r7 * z_moderate
            + r8 * z_deep
            + r9 * z_deep
        )

        denominator = (
            r1 + r2 + r3 + r4 + r5
            + r6 + r7 + r8 + r9
        )

        crisp_output = 0.0

        if denominator > 0:
            crisp_output = numerator / denominator

        # Keep output within 0-100%
        crisp_output = min(
            max(crisp_output, 0.0),
            100.0
        )

        draw_bar(
            "Knee Bend",
            crisp_output,
            100.0,
            "%"
        )

    print("\nPress Enter to exit...")

    try:
        input()
    except EOFError:
        pass


if __name__ == "__main__":
    main()