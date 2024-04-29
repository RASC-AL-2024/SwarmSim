import numpy as np
import matplotlib.pyplot as plt

data = np.loadtxt('resourceData.csv', delimiter=',', skiprows=1)

fig, (ax1, ax2) = plt.subplots(2)
ax1.plot(data[:, 0], data[:, 1])
ax1.set_ylabel("Energy J")
ax2.plot(data[:, 0], data[:, 2])
ax2.set_ylabel("Completed modules")
plt.show()

