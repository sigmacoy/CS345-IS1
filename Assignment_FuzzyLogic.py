import sys
import math

def triangular_membership(x, a, b, c):
    """
    Helper function for Triangular Membership Function
    Parameters: x (input value), a (left foot), b (peak), c (right foot)
    """
    if x <= a or x >= c:
        return 0.0
    if x == b:
        return 1.0
    if x > a and x < b:
        return (x - a) / (b - a)
    return (c - x) / (c - b)

def draw_bar(label, value, max_val, unit=""):
    """
    Draws a visual text bar for the console output.
    Example: Height: [====  ] 2m
    """
    bar_length = 20
    filled = int((value / max_val) * bar_length)
    filled = min(max(filled, 0), bar_length)
    bar = "=" * filled + " " * (bar_length - filled)
    print(f"{label:<12}: [{bar}] {value:.2f}{unit}")

def main():
    print("=== BMX Landing Impact Calculator ===")
    
    try:
        height_input = input("Enter Drop Height (0 to 5m): ")
        height = float(height_input)
        height = min(max(height, 0.0), 5.0) # clamp to 0-5
        
        speed_input = input("Enter Landing Speed (0 to 40km/h): ")
        speed = float(speed_input)
        speed = min(max(speed, 0.0), 40.0) # clamp to 0-40
    except ValueError:
        print("Invalid input! Please enter numeric values.")
        return
        
    print("\nSelect Logic:")
    print("a - Mamdani")
    print("b - Sugeno")
    logic = input("Choice (a/b): ").strip().lower()

    if logic not in ['a', 'b']:
        print("Invalid choice.")
        return

    # Visuals of inputs
    print("\n--- Inputs ---")
    draw_bar("Height", height, 5.0, "m")
    draw_bar("Speed", speed, 40.0, "km/h")

    # 1. FUZZIFICATION
    # Drop Height Memberships (Low, Medium, High)
    h_low = triangular_membership(height, -1, 0, 2.5)
    h_med = triangular_membership(height, 0, 2.5, 5)
    h_high = triangular_membership(height, 2.5, 5, 6)

    # Landing Speed Memberships (Slow, Medium, Fast)
    s_slow = triangular_membership(speed, -1, 0, 20)
    s_med = triangular_membership(speed, 0, 20, 40)
    s_fast = triangular_membership(speed, 20, 40, 41)

    print("\n--- Fuzzification Results ---")
    print(f"Height [Low: {h_low:.2f}, Med: {h_med:.2f}, High: {h_high:.2f}]")
    print(f"Speed  [Slow: {s_slow:.2f}, Med: {s_med:.2f}, Fast: {s_fast:.2f}]")
 
    # 2. RULE EVALUATION
    # Rule 1: IF Height is High OR Speed is Fast, THEN Bend is Deep
    # Rule 2: IF Height is Medium AND Speed is Medium, THEN Bend is Moderate
    # Rule 3: IF Height is Low AND Speed is Slow, THEN Bend is Stiff
    rule1_strength = max(h_high, s_fast)    # Deep
    rule2_strength = min(h_med, s_med)      # Moderate
    rule3_strength = min(h_low, s_slow)     # Stiff

    print("\n--- Rule Firing Strengths ---")
    print(f"Rule 1 (Deep): {rule1_strength:.2f}")
    print(f"Rule 2 (Moderate): {rule2_strength:.2f}")
    print(f"Rule 3 (Stiff): {rule3_strength:.2f}")

    if logic == 'a':
        # MAMDANI DEFUZZIFICATION
        print("\n=== Mamdani Output ===")
        # Output memberships (Knee Bend Absorption 0 to 100%)
        sumNumerator = 0.0
        sumDenominator = 0.0
        step = 0.5
        
        y = 0.0
        while y <= 100.0:
            outStiff = triangular_membership(y, -1, 0, 50.0)
            outModerate = triangular_membership(y, 25.0, 50.0, 75.0)
            outDeep = triangular_membership(y, 50.0, 100.0, 101.0)
            
            clippedStiff = min(rule3_strength, outStiff)
            clippedModerate = min(rule2_strength, outModerate)
            clippedDeep = min(rule1_strength, outDeep)
            
            aggregatedY = max(clippedStiff, max(clippedModerate, clippedDeep))
            
            sumNumerator += y * aggregatedY * step
            sumDenominator += aggregatedY * step
            
            y += step
            
        crispOutput = 0.0
        if sumDenominator > 0.0:
            crispOutput = sumNumerator / sumDenominator
            
        draw_bar("Knee Bend", crispOutput, 100.0, "%")

    elif logic == 'b':
        print("\n=== Sugeno Output ===")
        # Sugeno output equations
        # Deep Bend uses strict math formula from requirements:
        z_deep = (height * 15) + (speed * 0.5)
        # We model the other rules with modified constants for a coherent system
        z_moderate = (height * 10) + (speed * 0.5)
        z_stiff = (height * 5) + (speed * 0.5)
        
        numerator = (rule1_strength * z_deep) + (rule2_strength * z_moderate) + (rule3_strength * z_stiff)
        denominator = rule1_strength + rule2_strength + rule3_strength
        
        crispOutput = 0.0
        if denominator > 0:
            crispOutput = numerator / denominator
            
        crispOutput = min(max(crispOutput, 0.0), 100.0) # limit to 0-100% bounds
        # 100% bend means a full maximum tuck.
        draw_bar("Knee Bend", crispOutput, 100.0, "%")

    print("\nPress Enter to exit...")
    try:
        input()
    except EOFError:
        pass

if __name__ == "__main__":
    main()