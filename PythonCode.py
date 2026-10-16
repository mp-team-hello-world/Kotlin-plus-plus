def process_data(limit: int) -> int:
    result: int = 0

    for i in range(1, limit):
        if i > 10 and limit > 1000:
            break
        elif i == 5:
            result += 10
        else:
            result = result + i * i

    while result < 5000:
        try:
            result *= 2
        except Exception as e:
            return -1
        finally:
            result += 1

    return result

score: int = process_data(20)